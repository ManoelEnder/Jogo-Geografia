using System;

namespace NucleoVivo
{
    /// <summary>Renascimento: cálculo de Núcleos, reinício da campanha e melhorias permanentes.</summary>
    public class RebirthSystem
    {
        readonly GameConfig config;
        readonly Func<GameState> stateGetter;
        GameState S => stateGetter();

        public RebirthSystem(GameConfig config, Func<GameState> state)
        {
            this.config = config;
            stateGetter = state;
        }

        public int NucleosFor(double produced)
        {
            if (produced <= 0 || config.rebirthThreshold <= 0) return 0;
            return (int)Math.Floor(Math.Sqrt(produced / config.rebirthThreshold) + 1e-9);
        }

        public int PendingNucleos => NucleosFor(S.campaignProduced);
        public bool CanRebirth => PendingNucleos >= 1;
        public double Progress01 => config.rebirthThreshold <= 0 ? 1 : Math.Min(1, S.campaignProduced / config.rebirthThreshold);

        /// <summary>Energia total da campanha necessária para o próximo Núcleo.</summary>
        public double NextNucleoAt
        {
            get
            {
                int n = PendingNucleos + 1;
                return n * (double)n * config.rebirthThreshold;
            }
        }

        public double RemainingForNextNucleo => Math.Max(0, NextNucleoAt - S.campaignProduced);

        public int Perform()
        {
            int gained = PendingNucleos;
            if (gained < 1) return 0;

            S.nucleos += gained;
            S.nucleosEarnedTotal += gained;
            S.rebirths++;
            S.generation++;
            if (S.campaignProduced > S.bestCampaignProduced) S.bestCampaignProduced = S.campaignProduced;
            if (S.fastestRebirthSeconds <= 0 || S.campaignSeconds < S.fastestRebirthSeconds) S.fastestRebirthSeconds = S.campaignSeconds;

            // Reinicia a campanha. Núcleos, melhorias permanentes, estatísticas e opções são mantidos.
            S.energy = 0;
            S.campaignProduced = 0;
            S.campaignSeconds = 0;
            S.upgradeLevels.Clear();
            S.reputation = config.startReputation;
            S.effects.Clear();
            S.eventsEnabled = false;
            S.eventTimer = 0;
            S.pendingEventId = "";
            S.pendingEventAge = 0;
            S.lastEventId = "";
            return gained;
        }

        public int PermanentLevel(PermanentUpgradeDef p) => S.GetPermanent(p.id);
        public int PermanentCost(PermanentUpgradeDef p) => p.baseCost + p.costStep * PermanentLevel(p);
        public bool PermanentMaxed(PermanentUpgradeDef p) => p.maxLevel > 0 && PermanentLevel(p) >= p.maxLevel;
        public bool CanBuyPermanent(PermanentUpgradeDef p) => !PermanentMaxed(p) && S.nucleos >= PermanentCost(p);

        public bool BuyPermanent(PermanentUpgradeDef p)
        {
            if (!CanBuyPermanent(p)) return false;
            S.nucleos -= PermanentCost(p);
            S.SetPermanent(p.id, PermanentLevel(p) + 1);
            return true;
        }

        public static string DescribePermanent(PermanentUpgradeDef p, int level)
        {
            switch (p.kind)
            {
                case PermanentKind.ClickBonus: return "Energia por toque +" + NumberFormat.Percent(p.valuePerLevel * level);
                case PermanentKind.PassiveBonus: return "Produção passiva +" + NumberFormat.Percent(p.valuePerLevel * level);
                case PermanentKind.EarlyDiscount: return "−" + NumberFormat.Percent(Math.Min(0.9f, p.valuePerLevel * level)) + " no custo das " + p.affectsFirstUpgrades + " primeiras melhorias";
                default: return "";
            }
        }
    }
}
