using System;
using System.IO;
using UnityEngine;

namespace NucleoVivo
{
    /// <summary>Salva o estado em JSON no persistentDataPath (com cópia de segurança).</summary>
    public static class SaveSystem
    {
        public const string FileName = "nucleovivo_save.json";

        public static string PathFor(string fileName = FileName) => Path.Combine(Application.persistentDataPath, fileName);

        public static void Save(GameState state, string fileName = FileName)
        {
            try
            {
                state.lastSaveUtc = DateTime.UtcNow.ToString("o");
                string json = JsonUtility.ToJson(state);
                string path = PathFor(fileName);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(path)) File.Copy(path, path + ".bak", true);
                File.Copy(tmp, path, true);
                File.Delete(tmp);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Núcleo Vivo] Não foi possível salvar: " + ex.Message);
            }
        }

        public static GameState Load(GameConfig config, string fileName = FileName)
        {
            string path = PathFor(fileName);
            GameState s = TryRead(path) ?? TryRead(path + ".bak");
            if (s == null)
            {
                s = new GameState { reputation = config.startReputation };
            }
            Sanitize(s);
            return s;
        }

        static GameState TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonUtility.FromJson<GameState>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Núcleo Vivo] Save corrompido em " + path + ": " + ex.Message);
                return null;
            }
        }

        static void Sanitize(GameState s)
        {
            if (s.upgradeLevels == null) s.upgradeLevels = new System.Collections.Generic.List<IdLevel>();
            if (s.permanentLevels == null) s.permanentLevels = new System.Collections.Generic.List<IdLevel>();
            if (s.openAreas == null) s.openAreas = new System.Collections.Generic.List<string>();
            if (s.effects == null) s.effects = new System.Collections.Generic.List<ActiveEffect>();
            if (s.pendingEventId == null) s.pendingEventId = "";
            if (s.lastEventId == null) s.lastEventId = "";
            if (double.IsNaN(s.energy) || s.energy < 0) s.energy = 0;
            if (double.IsNaN(s.campaignProduced) || s.campaignProduced < 0) s.campaignProduced = 0;
            s.reputation = Mathf.Clamp(s.reputation, 0, 100);
            if (s.generation < 1) s.generation = 1;
        }

        public static void Delete(string fileName = FileName)
        {
            string path = PathFor(fileName);
            foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                if (File.Exists(p)) File.Delete(p);
        }
    }
}
