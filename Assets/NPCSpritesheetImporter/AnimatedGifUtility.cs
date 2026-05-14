using AnimatedGif;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using UnityEngine;
using uGIF;


public static class AnimatedGifUtility {
    /// <summary>
    /// Creates an animated gif from textures.
    /// </summary>
    /// <param name="frames">Frames in order.</param>
    /// <param name="outputPath">Full output path. Example: Application.dataPath + "/test.gif"</param>
    /// <param name="frameDelayMs">Delay per frame in milliseconds.</param>
    /// <param name="loop">True = infinite loop.</param>
    public static void CreateGif(
        IList<Texture2D> frames, string outputPath, int frameDelayMs = 100) {

        GIFEncoder encoder = new GIFEncoder();


        MemoryStream m = new MemoryStream();

        encoder.Start(m);
        int i = 0;

        foreach (Texture2D text in frames) {
            encoder.AddFrame(new uGIF.Image(text));
            Debug.Log(i.ToString());
            
        }

        encoder.Finish();

        byte[] b = m.GetBuffer();

        m.Close();

        File.WriteAllBytes(outputPath, b);

        Debug.Log("GIF saved to: " + outputPath);
    }






}