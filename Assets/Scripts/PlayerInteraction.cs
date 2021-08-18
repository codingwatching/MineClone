using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private Camera _mainCamera;
    private Vector2 _cameraCenter;
    
    private TerrainGen _terrainGen;
    private PlayerInventory _playerInventory;

    private Vector3Int prevLookedBlock;
    private GameObject blockHighlight;

    private float delayTimer;

    [SerializeField] private float mouseInputDelay = 0.05f; //Delay between mouse input being processed
    [SerializeField] private float maxHitDistance = 5f;
    [SerializeField] private InventoryView _inventoryView;
    [SerializeField] private GameObject blockHighlightPrefab;

    // Start is called before the first frame update
    void Start()
    {
        _terrainGen = FindObjectOfType<TerrainGen>();
        _playerInventory = FindObjectOfType<PlayerInventory>();
        _mainCamera = Camera.main;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        _cameraCenter = new Vector2(_mainCamera.pixelWidth / 2, _mainCamera.pixelHeight / 2);
        delayTimer += Time.deltaTime;
        if (delayTimer >= mouseInputDelay)
        {
            ProcessMouseInput();
            //Reset delay timer only after block change
        }

        ProcessBlockHighlight();
    }

    void ProcessBlockHighlight()
    {
        RegionChunk collidedChunk = null;
        Vector3Int blockCoord = new Vector3Int();
        Vector3 adjustedHitCoord = new Vector3();
        RaycastHit hit;
        Ray ray = _mainCamera.ScreenPointToRay(_cameraCenter);
        var hitSomething = Physics.Raycast(ray, out hit);
        if (hitSomething && hit.distance < maxHitDistance)
        {
            //Make sure the coordinate to get the block is the correct block, thus the -0.5f
            adjustedHitCoord = hit.point + hit.normal * -0.5f;
            //Local within chunk (0-15) block coordinate
            blockCoord = new Vector3Int(Mathf.RoundToInt(adjustedHitCoord.x), Mathf.RoundToInt(adjustedHitCoord.y),
                Mathf.RoundToInt(adjustedHitCoord.z));

            if (blockHighlight == null)
            {
                blockHighlight = Instantiate(blockHighlightPrefab);
            }
            if (prevLookedBlock == null || blockCoord != prevLookedBlock)
            {
                blockHighlight.transform.position = blockCoord;
                blockHighlight.transform.rotation = Quaternion.identity;
                prevLookedBlock = blockCoord;
            } 
        }
        else
        {
            if (blockHighlight != null)
            {
                Destroy(blockHighlight);
            }
        }
    }

    void ProcessMouseInput()
    {
        bool blockChange = false;
        RegionChunk collidedChunk = null;
        Vector3Int blockCoord = new Vector3Int();
        Vector3 adjustedHitCoord = new Vector3();
        if ((Input.GetButton("Fire1") || Input.GetButton("Fire2")) && !_inventoryView.IsInventoryActive)
        {
            RaycastHit hit;
            Ray ray = _mainCamera.ScreenPointToRay(_cameraCenter);
            var hitSomething = Physics.Raycast(ray, out hit);
            if (hitSomething && hit.distance < maxHitDistance)
            {
                collidedChunk = hit.collider.transform.parent.GetComponent<RegionChunk>();

                if (Input.GetAxis("Fire1")>0)
                {
                    //Make sure the coordinate to get the block is the correct block, thus the -0.5f
                    adjustedHitCoord = hit.point + hit.normal * -0.5f;
                    //Local within chunk (0-15) block coordinate
                    blockCoord = WorldCoordToChunkBlockCoord(adjustedHitCoord);
                    //Possibly keep the record of modified blocks in TerrainGen
                    collidedChunk.BlocksData[blockCoord.x + 1][blockCoord.y][blockCoord.z + 1].BlockType = BlockTypes.AIR;
                }
                else
                {
                    if (_playerInventory.InventoryItems[_playerInventory.ActiveItemIndex].itemContained == null ||
                        _playerInventory.InventoryItems[_playerInventory.ActiveItemIndex].itemCount == 0)
                    {
                        return;
                    }
                    var placedBlockType = _playerInventory.InventoryItems[_playerInventory.ActiveItemIndex].itemContained.PlacedBlock;
                    if (placedBlockType == BlockTypes.NONE)
                    {
                        return;
                    }


                    Sides placedDirection = ConvertVectorToSide(hit.normal);
                    
                    //Make sure the coordinate to get the block is the correct block, thus the +0.5f
                    adjustedHitCoord = hit.point + hit.normal * 0.5f;
                    //Local within chunk (0-15) block coordinate
                    blockCoord = WorldCoordToChunkBlockCoord(adjustedHitCoord);
                    
                    
                    Vector3 castCenter = new Vector3(Mathf.RoundToInt(adjustedHitCoord.x), Mathf.RoundToInt(adjustedHitCoord.y),
                        Mathf.RoundToInt(adjustedHitCoord.z));
                    bool existsCollision = Physics.CheckBox(castCenter, new Vector3(0.48f, 0.48f, 0.48f));
                    if(existsCollision)
                        return;
                    
                    //Check if the block coord is in another chunk
                    if (blockCoord.x == 0 || blockCoord.x == RegionChunk.chunkSizeX - 1 || blockCoord.z == 0 ||
                        blockCoord.z == RegionChunk.chunkSizeZ - 1)
                    {
                        collidedChunk = _terrainGen.GetRegionChunk(TerrainGen.ChunkFromPosition(adjustedHitCoord));
                    }

                    //Possibly keep the record of modified blocks in TerrainGen
                    collidedChunk.BlocksData[blockCoord.x + 1][blockCoord.y][blockCoord.z + 1].BlockType = placedBlockType;
                    collidedChunk.BlocksData[blockCoord.x + 1][blockCoord.y][blockCoord.z + 1].UpDirection =
                        placedDirection;
                }
                blockChange = true;
            }
        }

        if (blockChange)
        {
            //Calculate
            StartCoroutine(collidedChunk.CalculateDrawnMesh(blockCoord.x / RenderChunk.xSize,
                blockCoord.y / RenderChunk.ySize, blockCoord.z / RenderChunk.zSize));

            if (RegionChunk
                .blockTypesProperties[collidedChunk.BlocksData[blockCoord.x + 1][blockCoord.y][blockCoord.z + 1]
                    .BlockType].isTransparent)
            {
                //Rerender the neighbouring render chunks
                switch (blockCoord.y % RenderChunk.ySize)
                {
                    case (0):
                        if (blockCoord.y / RenderChunk.ySize > 0)
                            StartCoroutine(collidedChunk.CalculateDrawnMesh(blockCoord.x / RenderChunk.xSize,
                                blockCoord.y / RenderChunk.ySize - 1, blockCoord.z / RenderChunk.zSize));
                        break;
                    case (RenderChunk.ySize - 1):
                        if (blockCoord.y / RenderChunk.ySize < RegionChunk.chunkSizeY / RenderChunk.ySize - 1)
                            StartCoroutine(collidedChunk.CalculateDrawnMesh(blockCoord.x / RenderChunk.xSize,
                                blockCoord.y / RenderChunk.ySize + 1, blockCoord.z / RenderChunk.zSize));
                        break;
                }

                //Extra logic needed to access the render chunk of different region chunk
                var blockCoordCopy = blockCoord;
                Vector2Int chunkID;
                RegionChunk regChunk;
                switch (blockCoord.x)
                {
                    case (0):
                        //Get the neighbouring chunk to update
                        chunkID = TerrainGen.ChunkFromPosition(new Vector3(adjustedHitCoord.x - 1f, adjustedHitCoord.y,
                            adjustedHitCoord.z));
                        regChunk = _terrainGen.GetRegionChunk(chunkID);
                        blockCoordCopy.x = RegionChunk.chunkSizeX - 1;
                        //Modify the block data copy in the neighbour chunk
                        regChunk.BlocksData[blockCoordCopy.x + 1 + 1][blockCoordCopy.y][blockCoordCopy.z + 1] =
                            collidedChunk.BlocksData[blockCoord.x + 1][blockCoord.y][blockCoord.z + 1];
                        //Recalculate the corresponding render chunk
                        StartCoroutine(regChunk.CalculateDrawnMesh(blockCoordCopy.x / RenderChunk.xSize,
                            blockCoordCopy.y / RenderChunk.ySize, blockCoordCopy.z / RenderChunk.zSize));
                        break;
                    case (RegionChunk.chunkSizeX - 1):
                        chunkID = TerrainGen.ChunkFromPosition(new Vector3(adjustedHitCoord.x + 1f, adjustedHitCoord.y,
                            adjustedHitCoord.z));
                        regChunk = _terrainGen.GetRegionChunk(chunkID);
                        blockCoordCopy.x = 0;
                        regChunk.BlocksData[blockCoordCopy.x + 1 - 1][blockCoordCopy.y][blockCoordCopy.z + 1] =
                            collidedChunk.BlocksData[blockCoord.x + 1][blockCoord.y][blockCoord.z + 1];
                        StartCoroutine(regChunk.CalculateDrawnMesh(blockCoordCopy.x / RenderChunk.xSize,
                            blockCoordCopy.y / RenderChunk.ySize, blockCoordCopy.z / RenderChunk.zSize));
                        break;
                }

                blockCoordCopy = blockCoord;
                switch (blockCoord.z)
                {
                    case (0):
                        chunkID = TerrainGen.ChunkFromPosition(new Vector3(adjustedHitCoord.x, adjustedHitCoord.y,
                            adjustedHitCoord.z - 1f));
                        regChunk = _terrainGen.GetRegionChunk(chunkID);
                        blockCoordCopy.z = RegionChunk.chunkSizeZ - 1;
                        regChunk.BlocksData[blockCoordCopy.x + 1][blockCoordCopy.y][blockCoordCopy.z + 1 + 1] =
                            collidedChunk.BlocksData[blockCoord.x + 1][blockCoord.y][blockCoord.z + 1];
                        StartCoroutine(regChunk.CalculateDrawnMesh(blockCoordCopy.x / RenderChunk.xSize,
                            blockCoordCopy.y / RenderChunk.ySize, blockCoordCopy.z / RenderChunk.zSize));
                        break;
                    case (RegionChunk.chunkSizeZ - 1):
                        chunkID = TerrainGen.ChunkFromPosition(new Vector3(adjustedHitCoord.x, adjustedHitCoord.y,
                            adjustedHitCoord.z + 1f));
                        regChunk = _terrainGen.GetRegionChunk(chunkID);
                        blockCoordCopy.z = 0;
                        regChunk.BlocksData[blockCoordCopy.x + 1][blockCoordCopy.y][blockCoordCopy.z + 1 - 1] =
                            collidedChunk.BlocksData[blockCoord.x + 1][blockCoord.y][blockCoord.z + 1];
                        StartCoroutine(regChunk.CalculateDrawnMesh(blockCoordCopy.x / RenderChunk.xSize,
                            blockCoordCopy.y / RenderChunk.ySize, blockCoordCopy.z / RenderChunk.zSize));
                        break;
                }
            }
            delayTimer = 0;
        }
    }

    Vector3Int WorldCoordToChunkBlockCoord(Vector3 worldCoord)
    {
        Vector3Int res = new Vector3Int();
        res.x = Mathf.RoundToInt(worldCoord.x) % RegionChunk.chunkSizeX;
        res.y = Mathf.RoundToInt(worldCoord.y);
        res.z = Mathf.RoundToInt(worldCoord.z) % RegionChunk.chunkSizeZ;

        if (res.x < 0)
        {
            res.x += 16;
        }

        if (res.z < 0)
        {
            res.z += 16;
        }
        
        return res;
    }

    Sides ConvertVectorToSide(Vector3 vec)
    {
        if(vec.normalized == Vector3.up)
            return Sides.UP;
        if (vec.normalized == Vector3.right)
            return Sides.RIGHT;
        if (vec.normalized == Vector3.back)
            return Sides.FRONT;
        if (vec.normalized == Vector3.left)
            return Sides.LEFT;
        if (vec.normalized == Vector3.forward)
            return Sides.BACK;
        if (vec.normalized == Vector3.down)
            return Sides.DOWN;

        return Sides.UP;
    }
}
