using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AdaptivePerformance.Provider;

[CreateAssetMenu(menuName = "Data/SpriteAlphabet")]
public class SpriteAlphabet : ScriptableObject
{
    public int spaceWidth = 6;
    
    public List<Sprite> sprites;


    private Dictionary<char, Sprite> dict;


    public void BuildDictionary() {
         dict = new Dictionary<char, Sprite>();

        foreach (Sprite s in sprites) {
            Debug.Log(s.name);
            char c = char.ToLower(s.name[0]);
            dict.Add(c, s);
        }
        


    }

    public int getLengthOfWord(string str) {
        int total = 0;
        foreach (char c in str) {

            if (c == ' ') {
                total += spaceWidth;
                total++;
            } else {
                Sprite s = dict[char.ToLower(c)];
                total += (int)s.rect.width;
                total++;
            }
        }
        total--;
        return total;



    }

    public void Test() {
        MakeWordSprite("Shibu");
    }

    public static Texture2D CreateBlackOutline(Texture2D src, float alphaThreshold = 0.01f, int thickness = 1) {
        int w = src.width;
        int h = src.height;

        // Read source pixels
        Color32[] srcPx = src.GetPixels32();

        // Output starts fully transparent
        Color32[] outPx = new Color32[srcPx.Length];
        for (int i = 0; i < outPx.Length; i++)
            outPx[i] = new Color32(0, 0, 0, 0);

        bool IsOpaque(int x, int y) {
            int idx = y * w + x;
            return srcPx[idx].a >= (byte)(alphaThreshold * 255f);
        }

        // For every transparent pixel, if it's near an opaque pixel -> outline it
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                int idx = y * w + x;

                // Keep source opaque pixels as-is (optional; remove if you ONLY want the outline)
                if (srcPx[idx].a > 0) {
                    outPx[idx] = srcPx[idx];
                    continue;
                }

                bool nearOpaque = false;

                for (int oy = -thickness; oy <= thickness && !nearOpaque; oy++) {
                    int ny = y + oy;
                    if (ny < 0 || ny >= h) continue;

                    for (int ox = -thickness; ox <= thickness; ox++) {
                        int nx = x + ox;
                        if (nx < 0 || nx >= w) continue;
                        if (ox == 0 && oy == 0) continue;

                        if (IsOpaque(nx, ny)) {
                            nearOpaque = true;
                            break;
                        }
                    }
                }

                if (nearOpaque)
                    outPx[idx] = new Color32(0, 0, 0, 255); // solid black outline
            }
        }

        var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        outTex.SetPixels32(outPx);
        outTex.filterMode = FilterMode.Point;
        outTex.Apply();
        return outTex;
    }

    public static Color GetSpritePixel(Sprite sprite, int x = 0, int y = 0) {
        var tex = sprite.texture;

        // Sprite's rectangle within the texture (in pixels). May be trimmed/packed.
        Rect r = sprite.textureRect;

        int tx = Mathf.FloorToInt(r.x) + x;
        int ty = Mathf.FloorToInt(r.y) + y;

        // Safety clamp
        tx = Mathf.Clamp(tx, 0, tex.width - 1);
        ty = Mathf.Clamp(ty, 0, tex.height - 1);

        return tex.GetPixel(tx, ty); // texture must be Read/Write enabled
    }


    public Sprite MakeWordSprite(string str) {
        if (dict == null || dict.Count == 0) {
            BuildDictionary();
        }

        
        int total = getLengthOfWord(str);
        //Debug.Log("Total " + total);

        Texture2D tex = new Texture2D( total + 2, (int)sprites[0].rect.height + 2);

        for (int x = 0; x < tex.width; x++) {
            for (int y = 0; y < tex.height; y++) {
                tex.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
        }

                int startxPoint = 1;
        int startyPoint = 1;
        for (int charindex = 0; charindex < str.Length; charindex++) {

            char c = char.ToLower(str[charindex]);

            if (c != ' ') {
                Sprite s = dict[char.ToLower(c)];

                for (int x = 0; x < s.rect.width; x++) {
                    for (int y = 0; y < s.rect.height; y++) {


                        int translatedx = startxPoint + x;
                        int translatedy = startyPoint + y;

                        tex.SetPixel(translatedx, translatedy, GetSpritePixel(s, x, y));


                    }
                }
                startxPoint += ((int)s.rect.width + 1);
            } else {
                startxPoint += spaceWidth + 1;
            }

            


                

        }


        



        tex.filterMode = FilterMode.Point;
        

        tex.Apply();

        Texture2D outline = CreateBlackOutline(tex);




        return Sprite.Create(outline, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0));




         
    }



}
