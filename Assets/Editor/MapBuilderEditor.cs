using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor;
using System;
using UnityEditor.UIElements;
using System.IO;

public class MapBuilderEditor : EditorWindow
{
    private string[] references;
    private Tile[] tiles;
    #region Custom Grid Params
    private Transform parent;
    private Vector3 offset;
    private int griddimensions;
    private int cellWorldSize;
    public Transform Parent { get => parent; set => parent = value; }
    public Vector3 OriginOffset { get => offset; set { offset = value; if (offsetField.value != value) { offsetField.value = value; } } }
    public int Griddimensions { get => griddimensions; set { griddimensions = value; if (gridSize.value != value) { gridSize.value = value; } } }
    public int CellWorldSize { get => cellWorldSize; set { cellWorldSize = value; if (cellSizeField.value != value) { cellSizeField.value = value; } } }
    #endregion
    private IntegerField gridSize;
    private Vector3Field offsetField;
    private IntegerField cellSizeField;
    private List<GameObject> instantiatedObjects = new List<GameObject>();
    private Dictionary<Vector3, GameObject> backup = new Dictionary<Vector3, GameObject>();
    public int GridScale { get { return _gridScale; } set { if (OffsetX + value > initialGrid.Dimension) {OffsetX = initialGrid.Dimension - value; }
            if (OffsetY + value > initialGrid.Dimension) { OffsetY = initialGrid.Dimension - value; }
            _gridScale = value;
            CreateGridColumns(OffsetX, OffsetY);
        } }
    private int _gridScale = 5;
    private string path = "";
    #region UIElements Variables
    private Scroller horizontalScroller;
    private Scroller verticalScroller;
    private ListView tilelist;
    #endregion
    private CustomGrid initialGrid;
    private Vector2Int currentPivot;
    public int OffsetX { get { return currentPivot.x; } 
        set { if (value != currentPivot.x) {currentPivot.x = value; if (horizontalScroller.highValue != initialGrid.Dimension - GridScale)
                { horizontalScroller.highValue = initialGrid.Dimension - GridScale; } CreateGridColumns(OffsetX, OffsetY); } } }
    public int OffsetY { get { return (int) horizontalScroller.highValue - currentPivot.y; } set { if (value != currentPivot.y) { currentPivot.y = value;
                if (verticalScroller.highValue != initialGrid.Dimension - GridScale)
                { verticalScroller.highValue = initialGrid.Dimension - GridScale; }
                CreateGridColumns(OffsetX, OffsetY); } } }


    [MenuItem("Tools/Map Builder")]
    public static void ShowWindow()
    {
        var window = GetWindow<MapBuilderEditor>();
        window.titleContent = new GUIContent("Map Builder");
        window.minSize = new Vector2(1200, 670);
        window.maxSize = new Vector2(1200, 670);
    }

    private void OnEnable()
    {
        currentPivot = new Vector2Int(0, 0);
        initialGrid = new CustomGrid(Vector3.zero);
        #region InitialWindowSetup
        VisualTreeAsset original = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/levelDesigner.uxml");
        TemplateContainer treeAsset = original.CloneTree();
        StyleSheet ss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/MapBuilderStyleSheet.uss");
        rootVisualElement.styleSheets.Add(ss);
        rootVisualElement.Add(treeAsset);
        #endregion
        #region Component Fetching
        horizontalScroller = rootVisualElement.Q<Scroller>("HorizontalScroller");
        verticalScroller = rootVisualElement.Q<Scroller>("VerticalScoller");
        var createButton = rootVisualElement.Q<Button>("NewGridButton");
        var rButton = rootVisualElement.Q<Button>("CancelButton");
        var uxmlField = rootVisualElement.Query<TextField>().First();
        var scanButton = rootVisualElement.Q<Button>("Scan");
        var scaleScroll = rootVisualElement.Q<SliderInt>("Scale");
        var saveButton = rootVisualElement.Q<Button>("SaveButton");
        var parentField = rootVisualElement.Q<ObjectField>("parentSelector");
        var clearButton = rootVisualElement.Q<Button>("ClearButton");
        var spaceScanButton = rootVisualElement.Q<Button>("SpaceScanButton");
        cellSizeField = rootVisualElement.Q<IntegerField>("TileSize");
        gridSize = rootVisualElement.Q<IntegerField>("GridDimensions");
        offsetField = rootVisualElement.Q<Vector3Field>("Offsetvector");
        #endregion;
        parentField.objectType = typeof(Transform);
        uxmlField.value = path;
        horizontalScroller.highValue = initialGrid.Dimension - GridScale;
        verticalScroller.highValue = initialGrid.Dimension - GridScale;
        #region Adding callbacks
        rButton.clicked += BackUp;
        saveButton.clicked += Save;
        offsetField.RegisterValueChangedCallback<Vector3>((v) => { foreach (GameObject go in instantiatedObjects) { go.transform.position += v.newValue - OriginOffset; } ;
            OriginOffset = v.newValue;});
        cellSizeField.RegisterValueChangedCallback<int>((v) => { if (v.newValue > 0) { CellWorldSize = v.newValue; } else { cellSizeField.value = 1; } });
        spaceScanButton.clicked += SpaceScan;
        clearButton.clicked += Clear;
        createButton.clicked += NewGrid;
        gridSize.RegisterValueChangedCallback<int>((v) =>{ if (v.newValue > 0)
            {
                Griddimensions = v.newValue;
                if (v.newValue > v.previousValue)
                { initialGrid.IncreaseGridSize(v.newValue - v.previousValue); horizontalScroller.highValue = Griddimensions - GridScale; verticalScroller.highValue = Griddimensions - GridScale;}
                else { initialGrid.ReduceGridSize(v.previousValue - v.newValue); horizontalScroller.highValue = Griddimensions - GridScale; verticalScroller.highValue = Griddimensions - GridScale; }
                CreateGridColumns(OffsetX, OffsetY);
            }
            else { gridSize.value = 1; } });
        scaleScroll.RegisterValueChangedCallback((v) =>
        {
            GridScale = v.newValue;
        });
        scanButton.clicked += ScanForTiles;
        uxmlField.RegisterCallback<ChangeEvent<string>>((evt) =>
        {
            path = uxmlField.text;
        });
        horizontalScroller.valueChanged += (v) => { OffsetX = Mathf.RoundToInt(v); };
        verticalScroller.valueChanged += (v) => { OffsetY = Mathf.RoundToInt(v); };
        #endregion
        CreateGridColumns(OffsetX, OffsetY);
    }

    public void BackUp()
    {
        Debug.Log(backup.Count);
        if(backup.Count != 0)
        {
            foreach(KeyValuePair<Vector3, GameObject> go in backup)
            {
                Vector2Int pos = initialGrid.WorldToGridPosition(go.Key);
                Node currentNode = initialGrid.GetNode(pos);
                var ngo = Instantiate(go.Value);
                ngo.transform.position = go.Key;
                Tile tngo = ngo.GetComponent<Tile>();
                ngo.name = tngo.name + " (" + pos.x.ToString() + " " + pos.y.ToString() + ")";
                currentNode.tile = tngo;
                if(currentNode.instantiatedGO != null) DestroyImmediate(currentNode.instantiatedGO);
                currentNode.instantiatedGO = ngo;

            }
        }
        CreateGridColumns(OffsetX, OffsetY);
    }

    public void CreateGridColumns(int startingNodeX, int startingNodeY)
    {
        rootVisualElement.Q<Box>("TileMap").Clear();
        for (int i = 0; i < GridScale; i++)
        {
            Box box = new Box();
            box.name = "Column" + i.ToString();
            box.style.width = Mathf.Floor(601 / GridScale);
            box.style.height = 581;
            rootVisualElement.Q<Box>("TileMap").Add(box);
            CreateGridRows(initialGrid.GetNode(startingNodeX+i, startingNodeY), box.name);
        }
    }

    public void CreateGridRows(Node startingNode, string name)
    {
        rootVisualElement.Q<Box>(name).Clear();
        for (int i = GridScale-1; i>=0; i--)
        {
            Button button = new Button();
            try
            {                
                if (initialGrid.GetNode(startingNode.x, startingNode.y + i).tile != null)
                {
                    button.style.backgroundColor = initialGrid.GetNode(startingNode.x, startingNode.y + i).tile.editorColor;
                }
                else { button.style.backgroundColor = Color.white; }
            }
            catch (NullReferenceException) {return; }
            button.style.width = Mathf.Floor(601 / GridScale);
            button.style.height = Mathf.Floor(581 / GridScale);
            button.text = "(" + startingNode.x.ToString() + " " + (startingNode.y + i).ToString() + ")";
            rootVisualElement.Q<Box>(name).Add(button);
            int _i = i;
            button.clicked += () => {try { InstantiateObject(initialGrid.GetNode(startingNode.x, startingNode.y + _i), (Tile)tilelist.selectedItem, button); }
                catch (NullReferenceException) { Debug.LogWarning("Nothing selected"); }
                };
        }
    }
    
    public void Clear()
    {
        for (int i = instantiatedObjects.Count-1; i >= 0; i--)
        {

            backup.Add(instantiatedObjects[i].transform.position, tiles.First(x => x.notation == instantiatedObjects[i].GetComponent<Tile>().notation).gameObject);
            DestroyImmediate(instantiatedObjects[i]);
        }
        initialGrid = new CustomGrid(Vector3.zero); 
        CreateGridColumns(OffsetX, OffsetY);
    }

    public void NewGrid()
    {
        horizontalScroller.value = gridSize.value / 2;
        verticalScroller.value = gridSize.value / 2;
        initialGrid = new CustomGrid(offsetField.value, gridSize.value, CellWorldSize);
        Griddimensions = gridSize.value;
        OriginOffset = offsetField.value;
        CellWorldSize = cellSizeField.value;
        CreateGridColumns(OffsetX, OffsetY);
    }

    public void InstantiateObject(Node node, Tile tile, Button called)
    {
        if(node.tile != null)
        {
            instantiatedObjects.Remove(node.instantiatedGO);
            DestroyImmediate(node.instantiatedGO);
        }
        node.tile = tile;
        called.style.backgroundColor = tile.editorColor;
        var tileGO = Instantiate(((Tile)tilelist.selectedItem).gameObject);
        tileGO.transform.position = initialGrid.GetWorldPosition(node.x, node.y);
        tileGO.name = tile.name+ " (" + node.x.ToString() + " " + node.y.ToString()+")";
        node.instantiatedGO = tileGO;
        instantiatedObjects.Add(tileGO);
        if(Parent != null)
        {
            tileGO.transform.SetParent(Parent);
        }
    }

    public void Save()
    {
        string saveName = "test";
        if (!Directory.Exists("Assets/Resources/JSONDataLevelData/")) 
        {
            Directory.CreateDirectory("Assets/Resources/JSONDataLevelData/");
        }
        string savepath = "Assets/Resources/JSONDataLevelData/" + saveName + ".json";
        SaveFile save = new SaveFile
        {
            gridDimension = this.Griddimensions,
            cellSize = this.CellWorldSize,
            offset = new float[] {OriginOffset.x, OriginOffset.y, OriginOffset.z}
        };
        string str = JsonConvert.SerializeObject(save, Formatting.Indented);
        using (FileStream fs = new FileStream(savepath, FileMode.Create))
        {
            using (StreamWriter writer = new StreamWriter(fs))
            {
                writer.Write(str);
            }
        }
        UnityEditor.AssetDatabase.Refresh();
    }
    
    public void LoadSave(TextAsset saveJson)
    {
        SaveFile save = JsonConvert.DeserializeObject<SaveFile>(saveJson.ToString());
        OriginOffset = new Vector3(save.offset[0], save.offset[1], save.offset[2]);
        Griddimensions = save.gridDimension;
        CellWorldSize = save.cellSize;
        NewGrid();

    }
    

    public void SpaceScan() 
    {
        Debug.Log("Scanning");
        for(int i =0; i < initialGrid.Dimension; i++)
        {
            for (int k = 0; k < initialGrid.Dimension; k++)
            {
                RaycastHit hit;
                Physics.Raycast(initialGrid.GetWorldPosition(i, k) + Vector3.down, Vector3.up, out hit, 2f);
                Debug.Log(initialGrid.GetWorldPosition(i, k) + Vector3.down + " " + hit.collider != null);
                Tile newTile;
                if (hit.collider != null && hit.collider.TryGetComponent<Tile>(out newTile))
                {
                    Node newNode = new Node(i, k);
                    newNode.tile = newTile;
                    initialGrid.ChangeNode(i, k, newNode);
                    instantiatedObjects.Add(hit.collider.gameObject);
                }
            }
        }
        CreateGridColumns(OffsetX, OffsetY);
    }

    public void ScanForTiles()
    {
        FindAllTiles(out tiles);
        if (tiles.Length != 0)
        {
            ListView tilelist = rootVisualElement.Query<ListView>().First();
            tilelist.makeItem = () => new Label();
            tilelist.bindItem = (element, i) => (element as Label).text = tiles[i].name;
            tilelist.itemsSource = tiles;
            tilelist.itemHeight = 16;
            tilelist.selectionType = SelectionType.Single;
            this.tilelist = tilelist;
        }
        else { Debug.LogWarning("No suitable objects found, perhaps the given path is wrong"); }
    }
    
    public void FindAllTiles(out Tile[] tiles)
    {
        tiles = Resources.LoadAll<Tile>(path);
    }
}

