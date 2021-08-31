using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkUpdater : MonoBehaviour
{
    [SerializeField] private RegionChunk _regionChunk;
    [SerializeField] private float tickSpeed;
    public Queue<Vector3Int> updateCurrentTick = new Queue<Vector3Int>();
    public Queue<Vector3Int> updateNextTick = new Queue<Vector3Int>();

    public HashSet<Vector3Int> renderChunkToReDraw = new HashSet<Vector3Int>();
    
    
    private float tickTimer = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (tickTimer >= tickSpeed && updateNextTick.Count>0)
        {
            while (updateNextTick.Count > 0)
            {
                updateCurrentTick.Enqueue(updateNextTick.Dequeue());
            }
            ProcessTick();
            tickTimer = 0;
            updateNextTick.Clear();
        }

        tickTimer += Time.fixedDeltaTime;
    }

    void ProcessTick()
    {
        bool blockChanged = false;
        
        while (updateCurrentTick.Count > 0)
        {
            var processCoord = updateCurrentTick.Dequeue();
            var blockData = _regionChunk.BlocksData[processCoord.x + 1][processCoord.y][processCoord.z + 1];
            if (RegionChunk.blockTypesProperties[blockData.BlockType].BlockUpdate(_regionChunk, processCoord))
            {
                blockChanged = true;
            }
        }

        if (blockChanged)
        {
            //If block change, re render
            RedrawRenderChunks();
        }
    }

    void RedrawRenderChunks()
    {
        foreach (var idx in renderChunkToReDraw)
        {
            StartCoroutine(_regionChunk.CalculateDrawnMesh(idx.x, idx.y, idx.z));
        }
        renderChunkToReDraw.Clear();
    }
}
