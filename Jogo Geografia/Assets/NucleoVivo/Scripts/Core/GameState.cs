using System;
using System.Collections.Generic;

namespace NucleoVivo
{
    [Serializable]
    public class IdLevel
    {
        public string id;
        public int level;
    }

    [Serializable]
    public class ActiveEffect
    {
        public string sourceEventId;
        public string label;
        public EffectKind kind;
        public float value;
        public float duration;
        public float remaining;
    }

    /// <summary>Tudo o que é salvo. Serializado com JsonUtility.</summary>
    [Serializable]
    public class GameState
    {
        public int saveVersion = 1;

        // Campanha atual (reinicia no Renascimento)
        public double energy;
        public double campaignProduced;
        public List<IdLevel> upgradeLevels = new List<IdLevel>();
        public float reputation = 50;
        public List<ActiveEffect> effects = new List<ActiveEffect>();
        public bool eventsEnabled;
        public float eventTimer;
        public string pendingEventId = "";
        public float pendingEventAge;
        public string lastEventId = "";
        public double campaignSeconds;

        // Permanente
        public int nucleos;
        public List<IdLevel> permanentLevels = new List<IdLevel>();
        public int generation = 1;
        public bool tutorialDone;
        public bool soundOn = true;

        // Estatísticas históricas
        public double totalProducedAllTime;
        public long totalTaps;
        public int rebirths;
        public int nucleosEarnedTotal;
        public double playSeconds;
        public double bestCampaignProduced;
        public double fastestRebirthSeconds;
        public int eventsResolved;

        public string lastSaveUtc = "";

        public int GetLevel(string id) => Get(upgradeLevels, id);
        public void SetLevel(string id, int level) => Set(upgradeLevels, id, level);
        public int GetPermanent(string id) => Get(permanentLevels, id);
        public void SetPermanent(string id, int level) => Set(permanentLevels, id, level);

        static int Get(List<IdLevel> list, string id)
        {
            for (int i = 0; i < list.Count; i++) if (list[i].id == id) return list[i].level;
            return 0;
        }

        static void Set(List<IdLevel> list, string id, int level)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].id == id) { list[i].level = level; return; }
            list.Add(new IdLevel { id = id, level = level });
        }

        public bool HasPendingEvent => !string.IsNullOrEmpty(pendingEventId);
    }
}
