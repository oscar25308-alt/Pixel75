using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Menú: Tools > Aguila y Gemas > Crear Nivel1
// Corta los sprites, crea tags, tiles, prefabs y arma la escena Nivel1 completa.
public static class CrearNivel1
{
    const int Ancho = 48;
    const int Alto = 32;
    const string RutaSprites = "Assets/Sprites/";

    static Sprite[] spritesTiles;
    static readonly Dictionary<int, Tile> tiles = new Dictionary<int, Tile>();

    [MenuItem("Tools/Aguila y Gemas/Crear Nivel1")]
    public static void Crear()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // 1. Cortar spritesheets (Slice, PPU 16)
        Cortar("StaticTiles", 11, 13, 16, 16);
        Cortar("eagle-attack", 4, 1, 40, 41);
        Cortar("gem", 5, 1, 15, 13);
        Cortar("cherry", 5, 1, 21, 21);
        AssetDatabase.Refresh();

        spritesTiles = CargarSprites("StaticTiles");
        Sprite[] cuadrosAguila = CargarSprites("eagle-attack");
        Sprite[] cuadrosGema = CargarSprites("gem");
        Sprite[] cuadrosCereza = CargarSprites("cherry");

        // 2. Tags
        AsegurarTag("Gema");
        AsegurarTag("Cereza");

        // 3. Prefabs
        AsegurarCarpeta("Assets/Prefabs");
        AsegurarCarpeta("Assets/Tiles");
        AsegurarCarpeta("Assets/Scenes");
        GameObject prefabGema = CrearPrefab("Gema", cuadrosGema, 0.8f);
        GameObject prefabCereza = CrearPrefab("Cereza", cuadrosCereza, 1f);

        // 4. Escena nueva
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Tilemaps
        GameObject gridObj = new GameObject("Grid");
        gridObj.AddComponent<Grid>();
        Tilemap suelo = CrearTilemap(gridObj.transform, "Suelo", 0);
        Tilemap decoracion = CrearTilemap(gridObj.transform, "Decoracion", 1);

        System.Random azar = new System.Random(11);
        bool[,] ocupado = new bool[Ancho, Alto];
        for (int x = 0; x < Ancho; x++)
            for (int y = 0; y < Alto; y++)
            {
                ocupado[x, y] = x < 2 || y < 2 || x >= Ancho - 2 || y >= Alto - 2;
                int indice = azar.NextDouble() < 0.8 ? 0 : 1 + azar.Next(6);   // pasto con variaciones
                suelo.SetTile(new Vector3Int(x, y, 0), ObtenerTile(indice));
            }
        for (int dx = -3; dx <= 3; dx++)
            for (int dy = -3; dy <= 3; dy++)
                ocupado[24 + dx, 16 + dy] = true;   // zona libre donde aparece el águila

        // Borde visual (vallas)
        for (int x = 0; x < Ancho; x++)
        {
            decoracion.SetTile(new Vector3Int(x, 0, 0), ObtenerTile(34));
            decoracion.SetTile(new Vector3Int(x, Alto - 1, 0), ObtenerTile(34));
        }
        for (int y = 1; y < Alto - 1; y++)
        {
            decoracion.SetTile(new Vector3Int(0, y, 0), ObtenerTile(44));
            decoracion.SetTile(new Vector3Int(Ancho - 1, y, 0), ObtenerTile(46));
        }

        // Decoración: {indiceTile, dx, dy}
        Decorar(decoracion, azar, ocupado, new[] { new[] { 32, 0, 0 }, new[] { 21, 0, 1 } }, 1, 2, 16);                    // árboles
        Decorar(decoracion, azar, ocupado, new[] { new[] { 33, 0, 0 }, new[] { 34, 1, 0 }, new[] { 35, 2, 0 } }, 3, 1, 8); // vallas
        Decorar(decoracion, azar, ocupado, new[] { new[] { 70, 0, 0 } }, 1, 1, 5);                                         // barriles
        Decorar(decoracion, azar, ocupado, new[] { new[] { 71, 0, 0 } }, 1, 1, 4);
        Decorar(decoracion, azar, ocupado, new[] { new[] { 69, 0, 0 } }, 1, 1, 5);                                         // cajas
        Decorar(decoracion, azar, ocupado, new[] { new[] { 79, 0, 0 } }, 1, 1, 2);                                         // cofres
        Decorar(decoracion, azar, ocupado, new[] { new[] { 22, 0, 0 } }, 1, 1, 5);                                         // rocas
        Decorar(decoracion, azar, ocupado, new[] { new[] { 9, 0, 0 } }, 1, 1, 4);                                          // troncos
        Decorar(decoracion, azar, ocupado, new[] { new[] { 39, 0, 0 } }, 1, 1, 2);                                         // letreros

        // Gemas y cerezas
        PonerObjetos(prefabGema, "Gemas", 12, azar, ocupado);
        PonerObjetos(prefabCereza, "Cerezas", 7, azar, ocupado);

        // Bordes con collider (sin Trigger)
        CrearBorde("BordeArriba", new Vector2(Ancho / 2f, Alto - 0.5f), new Vector2(Ancho, 1f));
        CrearBorde("BordeAbajo", new Vector2(Ancho / 2f, 0.5f), new Vector2(Ancho, 1f));
        CrearBorde("BordeIzquierdo", new Vector2(0.5f, Alto / 2f), new Vector2(1f, Alto));
        CrearBorde("BordeDerecho", new Vector2(Ancho - 0.5f, Alto / 2f), new Vector2(1f, Alto));

        // Águila
        GameObject aguila = new GameObject("Aguila");
        aguila.transform.position = new Vector3(24.5f, 16.5f, 0f);
        aguila.transform.localScale = Vector3.one * 0.8f;
        SpriteRenderer dibujo = aguila.AddComponent<SpriteRenderer>();
        dibujo.sprite = cuadrosAguila[0];
        dibujo.sortingOrder = 10;
        Rigidbody2D rb = aguila.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;                 // no cae
        rb.freezeRotation = true;             // no gira
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BoxCollider2D caja = aguila.AddComponent<BoxCollider2D>();
        caja.size = new Vector2(1.25f, 1.25f);
        SpriteAnimado anim = aguila.AddComponent<SpriteAnimado>();
        anim.cuadros = cuadrosAguila;
        anim.fps = 8f;
        aguila.AddComponent<AguilaController>();

        // Cámara
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.2f, 0.35f, 0.2f);
        camObj.AddComponent<AudioListener>();
        camObj.transform.position = new Vector3(24.5f, 16.5f, -10f);
        CamaraSigue sigue = camObj.AddComponent<CamaraSigue>();
        SerializedObject so = new SerializedObject(sigue);
        so.FindProperty("objetivo").objectReferenceValue = aguila.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Guardar escena
        string rutaEscena = "Assets/Scenes/Nivel1.unity";
        EditorSceneManager.SaveScene(escena, rutaEscena);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(rutaEscena, true) };
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Listo", "Escena Nivel1 creada.\nPulsa Play y muévete con W, A, S, D.\nMira la ventana Console para el puntaje.", "OK");
    }

    // ---------- Utilidades ----------
    static void Cortar(string nombre, int columnas, int filas, int anchoCelda, int altoCelda)
    {
        string ruta = RutaSprites + nombre + ".png";
        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) throw new FileNotFoundException("No se encontró " + ruta + ". Copia los PNG a Assets/Sprites.");

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = 16;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.SaveAndReimport();

        SpriteDataProviderFactories fabrica = new SpriteDataProviderFactories();
        fabrica.Init();
        ISpriteEditorDataProvider datos = fabrica.GetSpriteEditorDataProviderFromObject(imp);
        datos.InitSpriteEditorDataProvider();

        List<SpriteRect> rects = new List<SpriteRect>();
        int altoTextura = filas * altoCelda;
        int n = 0;
        for (int f = 0; f < filas; f++)
            for (int c = 0; c < columnas; c++)
            {
                SpriteRect r = new SpriteRect();
                r.name = nombre + "_" + n;
                r.rect = new Rect(c * anchoCelda, altoTextura - (f + 1) * altoCelda, anchoCelda, altoCelda);
                r.alignment = SpriteAlignment.Center;
                r.pivot = new Vector2(0.5f, 0.5f);
                r.spriteID = GUID.Generate();
                rects.Add(r);
                n++;
            }
        datos.SetSpriteRects(rects.ToArray());

        ISpriteNameFileIdDataProvider ids = datos.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (ids != null)
            ids.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());

        datos.Apply();
        (datos.targetObject as AssetImporter).SaveAndReimport();
    }

    static Sprite[] CargarSprites(string nombre)
    {
        return AssetDatabase.LoadAllAssetsAtPath(RutaSprites + nombre + ".png")
            .OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
            .ToArray();
    }

    static void AsegurarTag(string tag)
    {
        if (!InternalEditorUtility.tags.Contains(tag)) InternalEditorUtility.AddTag(tag);
    }

    static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = Path.GetDirectoryName(ruta).Replace("\\", "/");
        AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
    }

    static GameObject CrearPrefab(string nombre, Sprite[] cuadros, float tamanoCollider)
    {
        GameObject go = new GameObject(nombre);
        go.tag = nombre;   // el tag se llama igual que el prefab: Gema / Cereza
        SpriteRenderer dibujo = go.AddComponent<SpriteRenderer>();
        dibujo.sprite = cuadros[0];
        dibujo.sortingOrder = 5;
        BoxCollider2D caja = go.AddComponent<BoxCollider2D>();
        caja.isTrigger = true;
        caja.size = new Vector2(tamanoCollider, tamanoCollider);
        SpriteAnimado anim = go.AddComponent<SpriteAnimado>();
        anim.cuadros = cuadros;
        anim.fps = 6f;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/" + nombre + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static Tilemap CrearTilemap(Transform padre, string nombre, int orden)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre);
        Tilemap mapa = go.AddComponent<Tilemap>();
        TilemapRenderer render = go.AddComponent<TilemapRenderer>();
        render.sortingOrder = orden;
        return mapa;
    }

    static TileBase ObtenerTile(int indice)
    {
        Tile tile;
        if (tiles.TryGetValue(indice, out tile)) return tile;
        string ruta = "Assets/Tiles/StaticTiles_" + indice + ".asset";
        tile = AssetDatabase.LoadAssetAtPath<Tile>(ruta);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, ruta);
        }
        tile.sprite = spritesTiles[indice];
        EditorUtility.SetDirty(tile);
        tiles[indice] = tile;
        return tile;
    }

    static void Decorar(Tilemap mapa, System.Random azar, bool[,] ocupado, int[][] piezas, int w, int h, int cantidad)
    {
        int puestos = 0, intentos = 0;
        while (puestos < cantidad && intentos < 800)
        {
            intentos++;
            int x = 2 + azar.Next(Ancho - 4 - w + 1);
            int y = 2 + azar.Next(Alto - 4 - h + 1);
            bool libre = true;
            for (int i = -1; i <= w && libre; i++)
                for (int j = -1; j <= h; j++)
                    if (ocupado[x + i, y + j]) { libre = false; break; }
            if (!libre) continue;

            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    ocupado[x + i, y + j] = true;
            foreach (int[] p in piezas)
                mapa.SetTile(new Vector3Int(x + p[1], y + p[2], 0), ObtenerTile(p[0]));
            puestos++;
        }
    }

    static void PonerObjetos(GameObject prefab, string grupo, int cantidad, System.Random azar, bool[,] ocupado)
    {
        GameObject padre = new GameObject(grupo);
        int puestos = 0, intentos = 0;
        while (puestos < cantidad && intentos < 3000)
        {
            intentos++;
            int x = 2 + azar.Next(Ancho - 4);
            int y = 2 + azar.Next(Alto - 4);
            if (ocupado[x, y]) continue;
            ocupado[x, y] = true;
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            obj.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);
            obj.transform.SetParent(padre.transform);
            puestos++;
        }
    }

    static void CrearBorde(string nombre, Vector2 posicion, Vector2 tamano)
    {
        GameObject go = new GameObject(nombre);
        go.transform.position = posicion;
        BoxCollider2D caja = go.AddComponent<BoxCollider2D>();
        caja.size = tamano;   // sin Trigger: bloquea al águila
    }
}
