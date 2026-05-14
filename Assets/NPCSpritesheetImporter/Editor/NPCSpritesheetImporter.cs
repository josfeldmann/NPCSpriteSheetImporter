
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Unity.SharpZipLib.Zip;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Aseprite;
using UnityEditor.U2D.Sprites;
using UnityEngine;



// This tool expects a texture of a specific size and layout.
// Make sure the spritesheet you are using has the same layout as the provided examples or this just wont work.


public class NPCSpritesheetImporter : EditorWindow {


    public const int TEXTUREWIDTH = 504;
    public const int TEXTUREHEIGHT = 416;

    public const int INDIVIDUALSPRITEWIDTH = 126;
    public const int INDIVIDUALSPRITEHEIGHT = 104;

    public const int INDIVIDUALFRAMEWIDTH = 18;
    public const int INDIVIDUALFRAMEHEIGHT = 26;

    Texture2D selectedTexture;

    NPCFrameDataHolder[] sprites;

    Vector2 scroll;

    public static string exportPath = "NPCSpritesheetImporter/NPCExports";


    public static string Base_Char_Animator_Path() {
        string s = "Assets\\NPCSpritesheetImporter\\Base_Char_Animator.controller";
        return s;
    }



    static AnimatorController baseAnimator;

    public static void LoadBaseAnimator() {
        baseAnimator = AssetDatabase.LoadAssetAtPath<AnimatorController>(Base_Char_Animator_Path());
        if (baseAnimator == null) {
            EditorUtility.DisplayDialog("Base Animation error!", "Default Base animator not found. take a look at this Base_Char_Animator_Path() and verify the url there. Otherwhise you'll have to manually add the animator to this window every time.", "OK");
        }
    }



    [MenuItem("Tools/NPCImporter")]
    public static void ShowWindow() {
        EditorWindow.GetWindow<NPCSpritesheetImporter>(false, "Chars Importer");
        LoadBaseAnimator();
       
         
    }

    void OnGUI() {
        selectedTexture = (Texture2D)EditorGUILayout.ObjectField(
        "Spritesheet",
        selectedTexture,
        typeof(Texture2D),
        false,
        GUILayout.ExpandWidth(true)
    );

        if (selectedTexture == null) {
            sprites = null;
        }


        GUILayout.Label("Make Animations");

        MakeSpriteDataButton();
        CreateSpritesButton();
        CreateAnimationsButton();
        
        

        GUILayout.Label("Create Export Package");
        CreateExportPackageButton();
        CreateGifThumbnailButton();
        


        if (sprites != null) {
            GUILayout.Space(10);
            GUILayout.Label("Preview", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            int columns = 4;
            float padding = 6f;

            int rows = Mathf.CeilToInt(sprites.Length / (float)columns);

            for (int row = 0; row < rows; row++) {
                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(800));

                for (int col = 0; col < columns; col++) {
                    int index = row * columns + col;
                    if (index < sprites.Length && sprites[index] != null) {
                        SpriteSheetImporterUtilityFunctions.DrawNPCFrameDataHolderField(sprites[index]);
                        //DrawSprite(rect, sprites[index].mainSprite);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }




    }



    public bool SpritesShowing() {
        return sprites != null;
    }

    public GUIContent MakeSpriteDataButtonContent = new GUIContent(
        "1. Make Sprite Data From Sheet",
        "Looks at sprite sheet and determines what NPCs are placed on it. DOES NOT create sprite or animation assets."
    );

    GUIContent MakeSpritesContent = new GUIContent(
        "2. Create Sprites",
        "Cuts up the input texture into all the sprites. Modifies the texture asset."
    );

    GUIContent CreateAnimationsGuiContent = new GUIContent(
        "3. Create Animation Assets",
        "Makes Animation assets based on the created sprites. Writes the animations to disk"
    );


    GUIContent CreateExportPackage = new GUIContent(
        "Create Export Package",
        "Creates a zip containing the unity files, spritesheet, and separate anim and trainer spritesheets."
    );



    GUIContent CreateGifThumbnailGuiContent = new GUIContent(
        "Create Gif Thumbnail",
        "Creates gif thumbnail for asset page"
    );

    public void CreateExportPackageButton() {
        EditorGUI.BeginDisabledGroup(!SpritesShowing());
        if (GUILayout.Button(CreateExportPackage)) {

            List<string> createdFiles = new List<string>();
            string targetFolder = Application.dataPath + "/" + exportPath + "/" + selectedTexture.name;

            if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

            string editorPath =  targetFolder + "/" + selectedTexture.name + "_Animations.png";
            CreateCompactSheetAtPath(editorPath);
            createdFiles.Add(editorPath);

            editorPath = targetFolder + "/" + selectedTexture.name + "_Trainers.png";
            CreateTrainerSheetAtPath(editorPath);
            createdFiles.Add(editorPath);
            
            editorPath = targetFolder + "/" + selectedTexture.name + ".png";
            File.WriteAllBytes(editorPath, selectedTexture.EncodeToPNG());
            createdFiles.Add(editorPath);



            editorPath = targetFolder + "/" + selectedTexture.name + ".zip";
            ZipUtility.CreateZip(editorPath, createdFiles.ToArray());

            foreach (string s in createdFiles) {
                File.Delete(s);
            }


        }
        EditorGUI.EndDisabledGroup();
    }


    public Texture2D MakeTexture(List<Sprite> portraits, List<Sprite> npcs) {

        Texture2D tex = new Texture2D(336, 336);



        Vector2Int start = new Vector2Int(16, tex.height + 16);

        foreach (Sprite s in portraits) {

            for (int x = 0; x < s.rect.width; x++) {
                for (int y = 0; y < s.rect.width; y++) {
                    Vector2Int v = start + new Vector2Int(x, y);

                    Color c = s.texture.GetPixel((int)s.rect.position.x + x, (int)s.rect.position.y + y);
                    if (c.a == 0) c = Color.white;

                    tex.SetPixel(v.x, v.y, c);


                }
            }
            start += new Vector2Int((int)s.rect.width + 16, 0);

        }



        tex.Apply();

        return tex;


    }

    public void CreateGifThumbnailButton() {
        EditorGUI.BeginDisabledGroup(!SpritesShowing());
        if (GUILayout.Button(CreateGifThumbnailGuiContent)) {

            List<string> createdFiles = new List<string>();
            string targetFolder = Application.dataPath + "/" + exportPath + "/" + selectedTexture.name +"gifexport/";

            if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

            string editorPath = targetFolder + "/" + selectedTexture.name + ".gif";

            //List<Texture2D> texs = new List<Texture2D>() { selectedTexture };

            //AnimatedGifUtility.CreateGif(texs, editorPath);

            List<Texture2D> texs = new List<Texture2D>();

            for (int i = 0; i < 4; i++) {

                Texture2D tex = MakeTexture(new List<Sprite>() {
                                                            sprites[i].trainerSprite,
                                                            sprites[i+1].trainerSprite,
                                                            sprites[i+2].trainerSprite,
                                                            sprites[i+3].trainerSprite
                                                            }, new List<Sprite>() {
                                                            sprites[i].walkingDownSprites[1],
                                                            sprites[i+1].walkingDownSprites[1],
                                                            sprites[i+2].walkingDownSprites[1],
                                                            sprites[i+3].walkingDownSprites[1]
                                                            }

                );
                texs.Add( tex );
            }

            int ii = 0;

            List<string> paths = new List<string>();

            foreach (Texture2D tex in texs) {
                string s = targetFolder + ii.ToString() + ".png";
                paths.Add(s);

                File.WriteAllBytes(s, tex.EncodeToPNG());
                ii++;
            }


            CreateGif("C:\\Program Files\\Aseprite\\Aseprite.exe", paths.ToArray(), editorPath);

            //Command Line here

            //File.WriteAllBytes(editorPath, tex.EncodeToPNG());
            //AnimatedGifUtility.CreateGif( texs, edi
        }
        EditorGUI.EndDisabledGroup();
    }





    public static void CreateGif(
    string asepriteExe,
    string[] pngFiles,
    string outputGif) {
        string inputs = string.Join(" ",
            pngFiles.Select(f => $"\"{f}\""));

        var process = new Process();

        process.StartInfo.FileName = asepriteExe;
        process.StartInfo.Arguments =
            $"-b {inputs} --save-as \"{outputGif}\"";

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        process.WaitForExit();
    }




    public void MakeSpriteDataButton() {
        EditorGUI.BeginDisabledGroup(selectedTexture == null);
        if (GUILayout.Button(MakeSpriteDataButtonContent)) {
            if (selectedTexture == null) {
                UnityEngine.Debug.LogError("Null Texture");
                return;
            } else {



                if (selectedTexture.width != TEXTUREWIDTH || selectedTexture.height != TEXTUREHEIGHT) {
                    UnityEngine.Debug.LogError("Texture Wrong Size");
                }

                sprites = new NPCFrameDataHolder[16];
                int idx = 0;
                for (int y = 3; y >= 0; y--) {
                    for (int x = 0; x < 4; x++) {


                        int startx = INDIVIDUALSPRITEWIDTH * x;
                        int starty = INDIVIDUALSPRITEHEIGHT * y;

                        int index = (idx * 4) + x;

                        NPCFrameDataHolder npcData = new NPCFrameDataHolder(startx, starty, selectedTexture, index);
                        npcData.MakeSprites();
                        sprites[index] = npcData;


                    }
                    idx++;
                }


            }
        }
        EditorGUI.EndDisabledGroup();
    }

    //
    public void CreateSpritesButton() {

        EditorGUI.BeginDisabledGroup(!SpritesShowing());


        if (GUILayout.Button(MakeSpritesContent)) {

            Sprite[] ss = SliceTexture(selectedTexture, sprites);

            int i = 0;
            foreach (NPCFrameDataHolder npc in sprites) {
                npc.ReassignSprites(ss, i);
                i++;
            }

        }
        EditorGUI.EndDisabledGroup();


    }





    //3

    public bool CreateAnimationCondition() {
        return !SpritesShowing() || sprites == null || !sprites[0].spritesCreated;
    }



    public void CreateAnimationsButton() {

        EditorGUI.BeginDisabledGroup(CreateAnimationCondition());

        if (GUILayout.Button(CreateAnimationsGuiContent)) {

            LoadBaseAnimator();

            string targetFolder = Application.dataPath + "/" + exportPath + "/" + selectedTexture.name;

            if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

            string editorPath = exportPath + "/" + selectedTexture.name;



            foreach (NPCFrameDataHolder h in sprites) {


                NPCTrainerVisual visual = ScriptableObject.CreateInstance<NPCTrainerVisual>();

                visual.key = h.key;
                visual.npc_name = h.key;
                visual.name = h.key + "_NPC";

                visual.dialogueFrameColor = h.color;
                visual.usesDialogueColor = true;

                visual.overworldTrainerSprite = h.mainSprite;
                visual.trainerSprite = h.trainerSprite;

                string folderForAsset = editorPath + "/" + h.key;
                UnityEngine.Debug.Log(Application.dataPath + "/" + folderForAsset);
                if (!Directory.Exists(Application.dataPath + "/" + folderForAsset)) Directory.CreateDirectory(Application.dataPath + "/" + folderForAsset);

                string fullPath = "Assets/" + folderForAsset + "/" + visual.name + ".asset";




                Dictionary<string, AnimationClip> clips = h.MakeAnimationClips("Assets/" + folderForAsset, visual.name);

                RuntimeAnimatorController animator = SpriteSheetImporterUtilityFunctions.CreateOverrideControllerAsset(baseAnimator, "Assets/" + folderForAsset + "/" + visual.name + "_Animator.controller", clips);
                visual.animator = animator;

                AssetDatabase.CreateAsset(visual, fullPath);
                AssetDatabase.SaveAssets();
            }
        }

        EditorGUI.EndDisabledGroup();


    }



    //2






    public static Sprite[] SliceTexture(Texture2D texture, NPCFrameDataHolder[] rects) {
        string path = AssetDatabase.GetAssetPath(texture);
        if (string.IsNullOrEmpty(path)) {
            UnityEngine.Debug.LogError("Texture must be a saved project asset.");
            return null;
        }

        // Make sure this texture imports as a multi-sprite texture
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) {
            UnityEngine.Debug.LogError("Could not get TextureImporter.");
            return null;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;

        // Use the modern sprite data provider API
        var factory = new SpriteDataProviderFactories();
        factory.Init();

        ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        var spriteRects = new List<SpriteRect>();

        int i = 0;
        foreach (NPCFrameDataHolder npc in rects) {

            List<Sprite> sprites = npc.GetAllSprites();
            int ii = 0;
            foreach (Sprite s in sprites) {
                spriteRects.Add(new SpriteRect {
                    name = texture.name + "_" + i + "_" + ii,
                    rect = s.rect,
                    alignment = SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0.0f)
                });
                ii++;
            }
            i++;

        }


        dataProvider.SetSpriteRects(spriteRects.ToArray());
        dataProvider.Apply();

        importer.SaveAndReimport();

        // Load the generated sprite sub-assets
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
        return assets.OfType<Sprite>().ToArray();
    }


    public void CreateTrainerSheetAtPath(string path) {


        int trainerSheetWidth = 64 * 4;

        Texture2D t = new Texture2D(trainerSheetWidth, trainerSheetWidth);

        for (int x = 0; x < 4; x++) {
            for (int y = 0; y < 4; y++) {

                int npcindex = (x * 4) + y;



                int startx = (64 * x);
                int startY = 64 * y;

                NPCFrameDataHolder npc = sprites[15 - ((y * 4) + (3 - x))];


                for (int y1 = 0; y1 < 64; y1++) {
                    for (int x1 = 0; x1 < 64; x1++) {
                     
                            t.SetPixel(startx + x1, startY + y1, selectedTexture.GetPixel((int)npc.trainerSprite.rect.position.x + x1, (int)npc.trainerSprite.rect.position.y + (y1)));
                       
                    }
                }

                t.Apply();

                File.WriteAllBytes(path, t.EncodeToPNG());




            }
        }


    }

    public void CreateCompactSheetAtPath(string path) {


        int animSheetWidth = (int)sprites[0].walkingDownSprites[0].rect.width * 3;
        int animSheetHeight = (int)sprites[0].walkingDownSprites[0].rect.height * 4;

        int width = animSheetWidth * 4;
        int height = animSheetHeight * 4;

        Texture2D t = new Texture2D((int)width, (int)height);


       

        for (int x = 0; x < 4; x++) {
            for (int y = 0; y < 4; y++) {

                int npcindex = (x * 4) + y;

                

                int startx =  (animSheetWidth * x);
                int startY =  animSheetHeight * y;

                NPCFrameDataHolder npc = sprites[15 - ((y * 4) + (3-x))];


                for (int y1 = 0; y1 < animSheetHeight; y1++) {
                    for (int x1 = 0; x1 < animSheetWidth; x1++) {
                       
                            t.SetPixel(startx + x1, startY + y1, selectedTexture.GetPixel((int)npc.exportSprite.rect.position.x + x1, (int)npc.exportSprite.rect.position.y + (y1)));
                        
                    }
                }

                t.Apply();

                File.WriteAllBytes(path, t.EncodeToPNG());


            }
        }


    }







    private void DrawSprite(Rect position, Sprite sprite) {
        Rect tr = sprite.textureRect;
        Texture2D tex = sprite.texture;

        // Convert sprite.textureRect from pixel coords into normalized UV coords
        Rect uv = new Rect(
            tr.x / tex.width,
            tr.y / tex.height,
            tr.width / tex.width,
            tr.height / tex.height
        );

        // GUI.DrawTextureWithTexCoords uses normalized texture coordinates
        GUI.DrawTextureWithTexCoords(position, tex, uv, true);
    }


}


public static class SpriteSheetImporterUtilityFunctions {


    public static AnimationClip CreateSpriteAnimationClip(Sprite sprite, float interval, string assetPath) {
        return CreateSpriteAnimationClip(new List<Sprite>() { sprite }, interval, assetPath);
    }

    public static AnimationClip CreateSpriteAnimationClip(List<Sprite> sprites, float interval, string assetPath) {
        if (sprites == null || sprites.Count == 0) {
            UnityEngine.Debug.LogError("CreateSpriteAnimationClip: sprites list is null or empty.");
            return null;
        }

        if (interval <= 0f) {
            UnityEngine.Debug.LogError("CreateSpriteAnimationClip: interval must be greater than 0.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(assetPath)) {
            UnityEngine.Debug.LogError("CreateSpriteAnimationClip: assetPath is null or empty.");
            return null;
        }

        AnimationClip clip = new AnimationClip();


        // frameRate is used by Unity's animation timeline/editor display.
        clip.frameRate = 1f / interval;

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];

        for (int i = 0; i < sprites.Count; i++) {
            keyframes[i] = new ObjectReferenceKeyframe {
                time = i * interval,
                value = sprites[i]
            };
        }

        // This binds to the SpriteRenderer's sprite reference.
        EditorCurveBinding spriteBinding = EditorCurveBinding.PPtrCurve(
            "",
            typeof(SpriteRenderer),
            "m_Sprite"
        );

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);



        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
        AssetDatabase.CreateAsset(clip, uniquePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return AssetDatabase.LoadAssetAtPath<AnimationClip>(uniquePath);
    }


    public static AnimatorOverrideController CreateOverrideControllerAsset(RuntimeAnimatorController baseController, string assetPath, Dictionary<string, AnimationClip> clips) {
        if (baseController == null) {
            UnityEngine.Debug.LogError("CreateOverrideControllerAsset: baseController is null.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(assetPath)) {
            UnityEngine.Debug.LogError("CreateOverrideControllerAsset: assetPath is null or empty.");
            return null;
        }

        // Create an override controller that uses the passed controller as its base.
        AnimatorOverrideController overrideController =
            new AnimatorOverrideController(baseController);


        SetOverridesByName(overrideController, clips);

        // Make sure the asset path is unique.
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        // Save it as an asset.
        AssetDatabase.CreateAsset(overrideController, uniquePath);
        AssetDatabase.SaveAssets();

        // Return the saved asset reference.
        return AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(uniquePath);
    }


    public static void SetOverridesByName(
            AnimatorOverrideController overrideController,
            Dictionary<string, AnimationClip> replacementByOriginalClipName
        ) {
        if (overrideController == null) {
            UnityEngine.Debug.LogError("SetOverridesByName: overrideController is null.");
            return;
        }

        if (replacementByOriginalClipName == null) {
            UnityEngine.Debug.LogError("SetOverridesByName: replacementByOriginalClipName is null.");
            return;
        }

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
        overrideController.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++) {
            AnimationClip originalClip = overrides[i].Key;

            if (originalClip != null &&
                replacementByOriginalClipName.TryGetValue(originalClip.name, out AnimationClip newClip)) {
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, newClip);
            }
        }

        overrideController.ApplyOverrides(overrides);
    }


    public static void DrawNPCFrameDataHolderField(NPCFrameDataHolder data) {
        if (data == null) {
            EditorGUILayout.HelpBox("NPCFrameDataHolder is null.", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(200));

        EditorGUIUtility.labelWidth = 50;

        // Editable fields
        data.key = EditorGUILayout.TextField("Key", data.key);
        data.color = EditorGUILayout.ColorField("Color", data.color);

        //GUILayout.Space(8);

        // Sprite previews
        EditorGUILayout.LabelField("Sprites", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(200));

        DrawSpritePreviewBlock("Main Sprite", data.mainSprite, 96);
        //GUILayout.Space(8);
        DrawSpritePreviewBlock("Trainer Sprite", data.trainerSprite, 96);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private static void DrawSpritePreviewBlock(string label, Sprite sprite, float previewSize) {
        EditorGUILayout.BeginVertical();

        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

        Rect rect = GUILayoutUtility.GetRect(
            previewSize,
            previewSize,
            GUILayout.Width(previewSize),
            GUILayout.Height(previewSize)
        );

        EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f));

        if (sprite != null) {
            DrawSpriteInRect(rect, sprite);
        } else {
            GUI.Label(rect, "None", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private static void DrawSpriteInRect(Rect rect, Sprite sprite) {
        Texture2D tex = sprite.texture;
        Rect tr = sprite.textureRect;

        Rect uv = new Rect(
            tr.x / tex.width,
            tr.y / tex.height,
            tr.width / tex.width,
            tr.height / tex.height
        );

        // Preserve aspect ratio inside the preview rect
        float spriteAspect = tr.width / tr.height;
        Rect fitted = FitRect(rect, spriteAspect);

        GUI.DrawTextureWithTexCoords(fitted, tex, uv, true);
    }

    private static Rect FitRect(Rect outer, float aspect) {
        float outerAspect = outer.width / outer.height;

        if (aspect > outerAspect) {
            float height = outer.width / aspect;
            float y = outer.y + (outer.height - height) * 0.5f;
            return new Rect(outer.x, y, outer.width, height);
        } else {
            float width = outer.height * aspect;
            float x = outer.x + (outer.width - width) * 0.5f;
            return new Rect(x, outer.y, width, outer.height);
        }
    }
}


//Data class that represents each NPC. Stores the references to the original sprite images that are used to make the final animations.
[System.Serializable]
public class NPCFrameDataHolder {

    public string key;
    public Color color;


    public int startX;
    public int startY;

    public Texture2D texture;

    public Sprite mainSprite;
    public Sprite trainerSprite;
    public Sprite exportSprite;

    public List<Sprite> walkingDownSprites = new List<Sprite>();
    public List<Sprite> walkingLeftSprites = new List<Sprite>();
    public List<Sprite> walkingRightSprites = new List<Sprite>();
    public List<Sprite> walkingUpSprites = new List<Sprite>();



    public NPCTrainerVisual generatedVisual = null;


    internal string generatedVisualPath;

    public bool spritesCreated = false;

    public NPCFrameDataHolder(int startX, int startY, Texture2D texture, int index) {
        this.startX = startX;
        this.startY = startY;
        this.texture = texture;
        key = texture.name + "_" + index;
    }



    public Dictionary<string, AnimationClip> MakeAnimationClips(string folderPath, string name) {

        Dictionary<string, AnimationClip> dict = new Dictionary<string, AnimationClip>();

        float walkInterval = 0.25f;

        dict.Add("base_east_idle", SpriteSheetImporterUtilityFunctions.CreateSpriteAnimationClip(walkingRightSprites[1], walkInterval, folderPath + "/" + name + "_East_Idle.anim"));
        dict.Add("base_east_walk", SpriteSheetImporterUtilityFunctions.CreateSpriteAnimationClip(walkingRightSprites, walkInterval, folderPath + "/" + name + "_East_Walk.anim"));

        dict.Add("base_north_idle", SpriteSheetImporterUtilityFunctions.CreateSpriteAnimationClip(walkingUpSprites[1], walkInterval, folderPath + "/" + name + "_North_Idle.anim"));
        dict.Add("base_north_walk", SpriteSheetImporterUtilityFunctions.CreateSpriteAnimationClip(walkingUpSprites, walkInterval, folderPath + "/" + name + "_North_Walk.anim"));

        dict.Add("base_south_idle", SpriteSheetImporterUtilityFunctions.CreateSpriteAnimationClip(walkingDownSprites[1], walkInterval, folderPath + "/" + name + "_South_Idle.anim"));
        dict.Add("base_south_walk", SpriteSheetImporterUtilityFunctions.CreateSpriteAnimationClip(walkingDownSprites, walkInterval, folderPath + "/" + name + "_South_Walk.anim"));

        dict.Add("base_west_idle", SpriteSheetImporterUtilityFunctions.CreateSpriteAnimationClip(walkingLeftSprites[1], walkInterval, folderPath + "/" + name + "_West_Idle.anim"));
        dict.Add("base_west_walk", SpriteSheetImporterUtilityFunctions.CreateSpriteAnimationClip(walkingLeftSprites, walkInterval, folderPath + "/" + name + "_West_Walk.anim"));


        return dict;
    }

    public void MakeSprites() {

        walkingUpSprites = Make3Sprites(startX, startY);
        walkingRightSprites = Make3Sprites(startX, startY + NPCSpritesheetImporter.INDIVIDUALFRAMEHEIGHT);
        walkingLeftSprites = Make3Sprites(startX, startY + NPCSpritesheetImporter.INDIVIDUALFRAMEHEIGHT * 2);
        walkingDownSprites = Make3Sprites(startX, startY + NPCSpritesheetImporter.INDIVIDUALFRAMEHEIGHT * 3);
        exportSprite = Sprite.Create(texture, new Rect(startX, startY, NPCSpritesheetImporter.INDIVIDUALSPRITEWIDTH, NPCSpritesheetImporter.INDIVIDUALSPRITEHEIGHT), new Vector2(0.5f, 0));
        mainSprite = walkingDownSprites[1];

        trainerSprite = Sprite.Create(texture, new Rect(startX + 62, startY + 40, 64, 64), new Vector2(0.5f, 0.5f));

        Rect rect = trainerSprite.textureRect;

        int x = Mathf.FloorToInt(rect.x + rect.width * 0.5f);
        int y = Mathf.FloorToInt(rect.y + rect.height * 0.5f);
        color = texture.GetPixel(x, y);
        color = new Color(color.r, color.g, color.b, 1);


    }


    public List<Sprite> Make3Sprites(int sx, int sy) {
        List<Sprite> Sprites = new List<Sprite>();

        for (int i = 0; i < 3; i++) {
            Sprite s = Sprite.Create(texture, new Rect(sx + i * NPCSpritesheetImporter.INDIVIDUALFRAMEWIDTH, sy, NPCSpritesheetImporter.INDIVIDUALFRAMEWIDTH, NPCSpritesheetImporter.INDIVIDUALFRAMEHEIGHT), new Vector2(0.5f, 0));
            Sprites.Add(s);
        }
        return Sprites;
    }

    internal List<Sprite> GetAllSprites() {
        List<Sprite> list = new List<Sprite>(walkingDownSprites);
        list.AddRange(walkingRightSprites);
        list.AddRange(walkingLeftSprites);
        list.AddRange(walkingUpSprites);
        list.Add(trainerSprite);
        return list;
    }

    internal void ReassignSprites(Sprite[] ss, int i) {
        int startindex = 13 * i;

        if (key.Equals("Townies_0")) {
            for (int x = 0; x < 13; x++) {
                UnityEngine.Debug.Log(ss[x].name);
            }
        }


        walkingDownSprites = new List<Sprite>() { ss[startindex], ss[startindex + 1], ss[startindex + 2], ss[startindex + 1], };
        walkingRightSprites = new List<Sprite>() { ss[startindex + 3], ss[startindex + 4], ss[startindex + 5], ss[startindex + 4], };
        walkingLeftSprites = new List<Sprite>() { ss[startindex + 6], ss[startindex + 7], ss[startindex + 8], ss[startindex + 7] };
        walkingUpSprites = new List<Sprite>() { ss[startindex + 9], ss[startindex + 10], ss[startindex + 11], ss[startindex + 10] };
        trainerSprite = ss[startindex + 12];
        mainSprite = ss[startindex + 1];

        spritesCreated = true;
    }




}




public static class ZipUtility {
    public static void CreateZip(string zipPath, string[] files) {
        using (FileStream fs = File.Create(zipPath))
        using (ZipOutputStream zipStream = new ZipOutputStream(fs)) {
            zipStream.SetLevel(9); // 0-9 compression

            foreach (string file in files) {
                byte[] data = File.ReadAllBytes(file);

                ZipEntry entry = new ZipEntry(Path.GetFileName(file));
                entry.Size = data.Length;

                zipStream.PutNextEntry(entry);
                zipStream.Write(data, 0, data.Length);
                zipStream.CloseEntry();
            }

            zipStream.Finish();
        }
    }
}