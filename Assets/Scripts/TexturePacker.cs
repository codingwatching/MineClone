using System.Collections.Generic;
using UnityEngine;

public class TexturePacker : MonoBehaviour
{
    public Dictionary<BlockTypes, int> textureDictIndex;

    public Rect[] blockTextureRects;
    
    [SerializeField] public BlockTexturePair[] textureDict;

    [SerializeField] private Material renderChunkMaterial;

    void Awake()
    {
        packTextures();
        generateTextureDictIdx();
    }

    void Update()
    {
    }

    public void packTextures()
    {
        Texture2D resTexture = new Texture2D(1024,1024);
        List<Texture2D> texturesToPack = new List<Texture2D>();

        foreach (var t in textureDict)
        {
            texturesToPack.Add(t.blockTexture);
        }
        blockTextureRects = resTexture.PackTextures(texturesToPack.ToArray(), 2);
        resTexture.filterMode = FilterMode.Point;
        resTexture.wrapMode = TextureWrapMode.Clamp;
        renderChunkMaterial.SetTexture("_BaseMap", resTexture);
    }

    void generateTextureDictIdx()
    {
        textureDictIndex = new Dictionary<BlockTypes, int>();

        for (int i = 0; i < textureDict.Length; i++)
        {
            textureDictIndex.Add(textureDict[i].blockType, i);
        }
    } 
}
