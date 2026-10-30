using Rift;
using Rift.Audio;
using Rift.Extras;
using Rift.Levels;
using Rift.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rift.EditorTools
{
    /// <summary>
    /// Builds the "Extras" layer of a level: three souls, two talkers, the mimic, the watcher,
    /// a moving laser and a debris rain. Idempotent — run it again to rebuild the layer.
    /// </summary>
    public static class RiftExtrasBuilder
    {
        private const string RootName = "Extras";
        private const string MaterialFolder = "Assets/_RIFT/Materials/RiftExtras";

        [MenuItem("RIFT/Rebuild Level Extras")]
        public static void RebuildMenu()
        {
            string scene = SceneManager.GetActiveScene().name;
            int level = LevelIndex(scene);
            if (level <= 0)
            {
                EditorUtility.DisplayDialog("RIFT", "Open a Level01–Level04 scene first.", "OK");
                return;
            }

            string report = Install(level);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[RIFT extras] " + report);
        }

        public static string Install(int level)
        {
            EnsureMaterials();
            Sprite sprite = FindSquareSprite();
            if (sprite == null) return "Square_0 sprite not found.";
            Material npcMat = LoadMat("Npc"), soulMat = LoadMat("SoulOrb"), bubbleMat = LoadMat("Bubble"),
                     laserMat = LoadMat("Laser"), debrisMat = LoadMat("Debris");
            if (npcMat == null || soulMat == null || bubbleMat == null || laserMat == null || debrisMat == null)
                return "Extras materials could not be created.";

            GameObject old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject(RootName);
            var tracker = root.AddComponent<SoulTracker>();
            SetInt(tracker, "_total", 3);

            var souls = new GameObject("Souls").transform;
            souls.SetParent(root.transform, false);
            var npcs = new GameObject("Npcs").transform;
            npcs.SetParent(root.transform, false);
            var hazards = new GameObject("Hazards").transform;
            hazards.SetParent(root.transform, false);

            SoundBank bank = AssetDatabase.LoadAssetAtPath<SoundBank>("Assets/_RIFT/Data/SoundBank.asset");
            Transform goal = GameObject.Find("Goal") != null ? GameObject.Find("Goal").transform : null;
            Transform player = GameObject.Find("Player") != null ? GameObject.Find("Player").transform : null;

            LevelContent c = ContentFor(level, npcs, souls, hazards, sprite, npcMat, soulMat, bubbleMat, laserMat, debrisMat, bank);

            // --- souls -------------------------------------------------------------
            for (int i = 0; i < c.souls.Length; i++)
            {
                (float x, float maxY) = c.souls[i];
                float y = Surface(x, maxY) + 0.9f;
                GameObject soul = MakeSprite("Soul " + (i + 1), souls, sprite, soulMat, new Vector2(0.55f, 0.55f), 20);
                soul.transform.position = new Vector3(x, y, 0f);

                var circle = soul.AddComponent<CircleCollider2D>();
                circle.isTrigger = true;
                circle.radius = 1.3f;

                var pickup = soul.AddComponent<SoulPickup>();
                SetObj(pickup, "_bank", bank);
            }

            // --- hidden soul --------------------------------------------------------
            // Above the highest platform, right at the top of a full jump, and only real inside the lens.
            Bounds peak = default;
            bool hasPeak = false;
            foreach (RiftSolid solid in Object.FindObjectsByType<RiftSolid>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var r = solid.GetComponent<SpriteRenderer>();
                if (r == null || r.bounds.size.y > 3f || r.bounds.size.x > 12f) continue;
                if (!hasPeak || r.bounds.max.y > peak.max.y) { peak = r.bounds; hasPeak = true; }
            }
            if (hasPeak)
            {
                GameObject hiddenSoul = MakeSprite("Hidden Soul", souls, sprite, soulMat, new Vector2(0.55f, 0.55f), 20);
                hiddenSoul.transform.position = new Vector3(peak.center.x, peak.max.y + 4.2f, 0f);
                hiddenSoul.AddComponent<CircleCollider2D>().isTrigger = true;
                var hiddenPickup = hiddenSoul.AddComponent<SoulPickup>();
                hiddenPickup.hidden = true;
                SetObj(hiddenPickup, "_bank", bank);
            }

            // --- talkers -----------------------------------------------------------
            for (int i = 0; i < c.npcs.Length; i++)
            {
                (float x, float maxY, string[] lines) = c.npcs[i];
                float y = Surface(x, maxY) + 0.8f;

                var npcRoot = new GameObject("Talker " + (i + 1));
                npcRoot.transform.SetParent(npcs, false);
                npcRoot.transform.position = new Vector3(x, y, 0f);

                GameObject visual = MakeSprite("Visual", npcRoot.transform, sprite, npcMat, new Vector2(1f, 1.6f), 22);
                visual.transform.localPosition = Vector3.zero;

                var npc = npcRoot.AddComponent<TalkingNpc>();
                SetObj(npc, "_renderer", visual.GetComponent<SpriteRenderer>());
                SetObj(npc, "_visual", visual.transform);
                SetObj(npc, "_bubbleMaterial", bubbleMat);
                SetObj(npc, "_bank", bank);
                SetStrings(npc, "_lines", lines);
                SetStrings(npc, "_pesterLines", PestLines(level));
                SetString(npc, "_secret", "alucard chikour");
            }

            // --- guests ------------------------------------------------------------
            if (c.npcs.Length > 0)
            {
                string[] french = { "Oh la la, quelle catastrophe !", "C'est la vie, mon ami.", "Une baguette ? Non. Elle est a moi.", "Je suis riche. Et toi ?", "Ce n'est pas possible !", "Bonjour. Au revoir." };
                string[] fortune = { "\u516B\u65B9\u6765\u8D22" };
                (float x, float maxY, string[] _) = c.npcs[0];
                (float farX, float farMaxY, string[] __) = c.npcs[c.npcs.Length - 1];
                AddGuest("Beggar Woman", x - 6f, maxY, npcs, sprite, Variant("NpcBeggar", npcMat, 1f, new Color(0.13f, 0.1f, 0.08f)), bubbleMat, bank, fortune, fortune, fortune[0]);
                AddGuest("Rich Frenchman", farX + 6f, farMaxY, npcs, sprite, Variant("NpcRich", npcMat, 2f, new Color(0.08f, 0.07f, 0.16f)), bubbleMat, bank, french, french, "alucard chikour");
            }

            // --- mimic -------------------------------------------------------------
            {
                float y = Surface(c.fake.x, c.fake.maxY) + 0.9f;
                GameObject fake = MakeSprite("Fake Soul", souls, sprite, soulMat, new Vector2(0.55f, 0.55f), 20);
                fake.transform.position = new Vector3(c.fake.x, y, 0f);
                var mimic = fake.AddComponent<FakeSoul>();
                SetObj(mimic, "_bank", bank);
            }

            // --- watcher -----------------------------------------------------------
            {
                float y = Surface(c.watcher.x, c.watcher.maxY) + 0.75f;
                GameObject watcher = MakeSprite("Watcher", root.transform, sprite, npcMat, new Vector2(0.95f, 1.5f), 21);
                watcher.transform.position = new Vector3(c.watcher.x, y, 0f);
                var script = watcher.AddComponent<SuspiciousWatcher>();
                SetObj(script, "_goal", goal);
            }

            // --- laser -------------------------------------------------------------
            {
                float surfaceY = Surface(c.laser.x, c.laser.maxY);
                var laserRoot = new GameObject("Moving Laser");
                laserRoot.transform.SetParent(hazards, false);
                laserRoot.transform.position = new Vector3(c.laser.x, surfaceY, 0f);

                GameObject beam = MakeSprite("Beam", laserRoot.transform, sprite, laserMat, new Vector2(c.laser.width, 0.34f), 24);
                beam.transform.localPosition = Vector3.zero;

                var laser = laserRoot.AddComponent<MovingLaser>();
                SetObj(laser, "_beam", beam.GetComponent<SpriteRenderer>());
                SetFloat(laser, "_width", c.laser.width);
                SetFloat(laser, "_low", c.laser.low);
                SetFloat(laser, "_high", c.laser.high);
                SetFloat(laser, "_speed", 1.25f);
                SetFloat(laser, "_thickness", 0.2f);
            }

            // --- debris ------------------------------------------------------------
            {
                float surfaceY = Surface(c.debris.x, c.debris.maxY);
                var rain = new GameObject("Debris Rain");
                rain.transform.SetParent(hazards, false);
                rain.transform.position = new Vector3(c.debris.x, surfaceY, 0f);

                var script = rain.AddComponent<DebrisRain>();
                SetObj(script, "_sprite", sprite);
                SetObj(script, "_debrisMaterial", debrisMat);
                SetObj(script, "_markerMaterial", laserMat);
                SetFloat(script, "_width", c.debris.width);
                SetFloat(script, "_height", c.debris.height);
            }

            HudSouls(bank);

            var report = $"level {level}: 3 souls, {c.npcs.Length} talkers, mimic, watcher, laser, debris";
            if (player != null) report += $" (start x {player.position.x:0.0})";
            return report;
        }

        private struct LevelContent
        {
            public (float x, float maxY)[] souls;
            public (float x, float maxY, string[] lines)[] npcs;
            public (float x, float maxY) fake;
            public (float x, float maxY) watcher;
            public (float x, float maxY, float width, float low, float high) laser;
            public (float x, float maxY, float width, float height) debris;
        }

        private static LevelContent ContentFor(int level, Transform npcs, Transform souls, Transform hazards,
            Sprite sprite, Material npcMat, Material soulMat, Material bubbleMat, Material laserMat,
            Material debrisMat, SoundBank bank)
        {
            switch (level)
            {
                case 1:
                    return new LevelContent
                    {
                        souls = new[] { (43f, 6f), (61f, 8f), (107f, 10f) },
                        npcs = new[]
                        {
                            (21.5f, 4f, Lines1A()),
                            (93f, 6f, Lines1B()),
                        },
                        fake = (118f, 6f),
                        watcher = (5f, 6f),
                        laser = (80f, 5f, 6f, 0.55f, 2.0f),
                        debris = (114f, 6f, 9f, 12f),
                    };
                case 2:
                    return new LevelContent
                    {
                        souls = new[] { (26f, 0f), (56f, 10f), (115f, 25f) },
                        npcs = new[]
                        {
                            (15f, 0f, Lines2A()),
                            (85f, 22f, Lines2B()),
                        },
                        fake = (120f, 24f),
                        watcher = (5f, 6f),
                        laser = (32f, 0f, 6f, 0.55f, 2.0f),
                        debris = (80f, 20f, 8f, 10f),
                    };
                case 3:
                    return new LevelContent
                    {
                        souls = new[] { (27f, 5f), (64.5f, 18f), (142f, 14f) },
                        npcs = new[]
                        {
                            (8f, 5f, Lines3A()),
                            (113f, 20f, Lines3B()),
                        },
                        fake = (145f, 14f),
                        watcher = (5f, 6f),
                        laser = (39f, 4f, 6f, 0.55f, 2.0f),
                        debris = (171f, 14f, 9f, 10f),
                    };
                default:
                    return new LevelContent
                    {
                        souls = new[] { (15f, 5f), (72f, 0f), (178f, 5f) },
                        npcs = new[]
                        {
                            (6f, 5f, Lines4A()),
                            (150f, 5f, Lines4B()),
                        },
                        fake = (145f, 5f),
                        watcher = (6f, 6f),
                        laser = (68f, 0f, 7f, 0.6f, 2.6f),
                        debris = (178f, 5f, 9f, 10f),
                    };
            }
        }

        // --- level voice -----------------------------------------------------------
        private static string[] Lines1A() => new[]
        {
            "the bridge is only a suggestion. it holds if you don't look down.",
            "i've been standing here since the last time you were here.",
            "count the steps. now count them again. see?",
            "don't trust the floor. trust the part that stepped anyway.",
            "you keep coming back. i don't think that's the rift doing it.",
        };

        private static string[] Lines1B() => new[]
        {
            "i don't remember which side of the lens i'm on.",
            "the walls talk when you're not aiming at them.",
            "there was someone behind you a moment ago. don't turn around.",
            "if the ground disappears, that's just the level being honest.",
            "you're looking for a reason. the reason is the looking.",
        };

        private static string[] Lines2A() => new[]
        {
            "i left something down there. i don't want it back.",
            "the dark isn't empty. it's just very patient.",
            "you can carry it, or put it down. both cost something.",
            "the crumbles remember being a floor. be gentle.",
            "climbing is just falling with better manners.",
        };

        private static string[] Lines2B() => new[]
        {
            "the summit is real. the way up is the part you keep forgetting.",
            "someone built these steps out of almosts.",
            "if you close your eyes the gap gets wider.",
            "i've counted the fall. it's shorter than you think.",
            "accept the step. then the next one. that's the whole trick.",
        };

        private static string[] Lines3A() => new[]
        {
            "the glass is not as thin as it looks. or it is. look anyway.",
            "everyone down here is afraid. the brave ones are just louder.",
            "i watch the corridor. the corridor watches back.",
            "the seam is where they stitched the level together. step on it.",
            "you don't have to be ready. you just have to go.",
        };

        private static string[] Lines3B() => new[]
        {
            "the ceiling is a door that forgot which way it opens.",
            "the fear wall is only a wall from one side.",
            "i heard you land from three rooms away.",
            "the crumble bridge is honest. it tells you it's leaving.",
            "courage is a colour. you're wearing it.",
        };

        private static string[] Lines4A() => new[]
        {
            "nobody's coming. that's not a threat, it's a schedule.",
            "someone left a light on. i think it was for you.",
            "the cavern is where the level keeps what it couldn't say.",
            "reach. that's all. just reach.",
            "you're not the only one here. that's the whole trick.",
        };

        private static string[] Lines4B() => new[]
        {
            "the wall is lonely because it thinks it's the only wall.",
            "i saw a soul run away from you. good. it's learning.",
            "the high step is a question. the drop is the answer.",
            "someone is watching from somewhere strange. that's normal now.",
            "connection is just reaching with company.",
        };

        private static string[] PestLines(int level)
        {
            switch (level)
            {
                case 1: return new[] { "you're still pressing it.", "the key won't get tired.", "i can hear you thinking." };
                case 2: return new[] { "you're going to hurt your finger.", "i said it already.", "let go." };
                case 3: return new[] { "still here?", "you're braver than the key.", "the dark is flattered." };
                default: return new[] { "you can stop now.", "i heard you the first time.", "the echo is getting tired." };
            }
        }

        // --- assets ----------------------------------------------------------------
        private static void EnsureMaterials()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_RIFT/Materials"))
                    AssetDatabase.CreateFolder("Assets/_RIFT", "Materials");
                AssetDatabase.CreateFolder("Assets/_RIFT/Materials", "RiftExtras");
            }

            MakeMat("Npc", "RIFT/Npc");
            MakeMat("SoulOrb", "RIFT/SoulOrb");
            MakeMat("Bubble", "RIFT/Bubble");
            MakeMat("Laser", "RIFT/Laser");
            MakeMat("Debris", "RIFT/Debris");
            AssetDatabase.SaveAssets();
        }

        private static void MakeMat(string name, string shaderName)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("[RIFT extras] shader not found: " + shaderName);
                return;
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
                EditorUtility.SetDirty(mat);
            }
        }

        private static Material LoadMat(string name) =>
            AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/" + name + ".mat");

        private static Sprite FindSquareSprite()
        {
            const string sheet = "Assets/_RIFT/Art/Square.png";
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(sheet))
                if (asset is Sprite sprite && sprite.name == "Square_0") return sprite;

            foreach (string guid in AssetDatabase.FindAssets("t:Sprite"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset is Sprite sprite && sprite.name == "Square_0") return sprite;
            }
            return null;
        }

        // --- scene helpers ---------------------------------------------------------
        private static GameObject MakeSprite(string name, Transform parent, Sprite sprite, Material material,
            Vector2 scale, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = "World";
            renderer.sortingOrder = order;
            return go;
        }

        private static void AddGuest(string name, float x, float maxY, Transform parent, Sprite sprite, Material mat, Material bubble, SoundBank bank, string[] lines, string[] pester, string secret)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(x, Surface(x, maxY) + 0.8f, 0f);

            GameObject visual = MakeSprite("Visual", root.transform, sprite, mat, new Vector2(1f, 1.6f), 22);
            visual.transform.localPosition = Vector3.zero;

            var npc = root.AddComponent<TalkingNpc>();
            SetObj(npc, "_renderer", visual.GetComponent<SpriteRenderer>());
            SetObj(npc, "_visual", visual.transform);
            SetObj(npc, "_bubbleMaterial", bubble);
            SetObj(npc, "_bank", bank);
            SetStrings(npc, "_lines", lines);
            SetStrings(npc, "_pesterLines", pester);
            SetString(npc, "_secret", secret);
        }

        private static Material Variant(string name, Material source, float outfit, Color cloak)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(source);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetFloat("_Outfit", outfit);
            mat.SetColor("_BodyColor", cloak);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static float Surface(float x, float maxY)
        {
            float best = float.NegativeInfinity;
            foreach (RiftSolid solid in Object.FindObjectsByType<RiftSolid>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var renderer = solid.GetComponent<SpriteRenderer>();
                if (renderer == null) continue;
                Bounds b = renderer.bounds;
                if (x < b.min.x || x > b.max.x) continue;
                if (b.max.y > maxY + 0.001f) continue;
                if (b.max.y > best) best = b.max.y;
            }
            return best > float.NegativeInfinity ? best : 0f;
        }

        private static void HudSouls(SoundBank bank)
        {
            LevelHud hud = Object.FindFirstObjectByType<LevelHud>(FindObjectsInactive.Exclude);
            if (hud == null) return;
            Transform group = hud.transform.Find("HudGroup");
            if (group == null) return;

            for (int i = group.childCount - 1; i >= 0; i--)
            {
                Transform child = group.GetChild(i);
                if (child.name == "Souls" || child.name == "Souls Label")
                    Object.DestroyImmediate(child.gameObject);
            }

            Transform labelSource = group.Find("Deaths Label");
            Transform valueSource = group.Find("Deaths");
            if (labelSource == null || valueSource == null) return;

            GameObject label = Object.Instantiate(labelSource.gameObject, group, false);
            label.name = "Souls Label";
            label.GetComponent<TMP_Text>().text = "SOULS";
            ((RectTransform)label.transform).anchoredPosition = new Vector2(520f, -32f);

            GameObject value = Object.Instantiate(valueSource.gameObject, group, false);
            value.name = "Souls";
            value.GetComponent<TMP_Text>().text = "0 / 3";
            ((RectTransform)value.transform).anchoredPosition = new Vector2(518f, -54f);

            SetObj(hud, "_souls", value.GetComponent<TMP_Text>());
            EditorUtility.SetDirty(hud);
        }

        private static int LevelIndex(string sceneName)
        {
            if (sceneName.StartsWith("Level") && int.TryParse(sceneName.Substring(5), out int n)) return n;
            return 0;
        }

        // --- serialized field helpers ----------------------------------------------
        private static void SetObj(Component c, string field, Object value)
        {
            var so = new SerializedObject(c);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[RIFT extras] missing {c.GetType().Name}.{field}"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Component c, string field, string value)
        {
            var so = new SerializedObject(c);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) return;
            p.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStrings(Component c, string field, string[] values)
        {
            var so = new SerializedObject(c);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).stringValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Component c, string field, float value)
        {
            var so = new SerializedObject(c);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) return;
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Component c, string field, int value)
        {
            var so = new SerializedObject(c);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) return;
            p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
