using System;

namespace NucleoVivo
{
    /// <summary>Regras de produção e compra. Não depende de MonoBehaviour para poder ser testada.</summary>
    public class Economy
    {
        readonly GameConfig config;
        readonly Func<GameState> stateGetter;
        GameState S => stateGetter();

        public Economy(GameConfig config, Func<GameState> state)
        {
            this.config = config;
            stateGetter = state;
        }

        public int Level(int index) => S.GetLevel(config.upgrades[index].id);

        // ---------- multiplicadores ----------
        public double PermanentClickMultiplier => 1.0 + PermanentValue(PermanentKind.ClickBonus);
        public double PermanentPassiveMultiplier => 1.0 + PermanentValue(PermanentKind.PassiveBonus);

        public double PermanentValue(PermanentKind kind)
        {
            double total = 0;
            foreach (var p in config.permanentUpgrades)
                if (p.kind == kind) total += p.valuePerLevel * S.GetPermanent(p.id);
            return total;
        }

        public double DiscountFor(int index)
        {
            double discount = 0;
            foreach (var p in config.permanentUpgrades)
                if (p.kind == PermanentKind.EarlyDiscount && index < p.affectsFirstUpgrades)
                    discount += p.valuePerLevel * S.GetPermanent(p.id);
            return Math.Min(0.9, discount);
        }

        public double EffectMultiplier(EffectKind kind)
        {
            double m = 1;
            foreach (var e in S.effects) if (e.kind == kind) m *= e.value;
            return m;
        }

        // ---------- produção ----------
        public double BaseClick
        {
            get
            {
                double v = config.startClickValue;
                for (int i = 0; i < config.upgrades.Count; i++) v += config.upgrades[i].clickGain * Level(i);
                return v;
            }
        }

        public double BasePassive
        {
            get
            {
                double v = 0;
                for (int i = 0; i < config.upgrades.Count; i++) v += config.upgrades[i].passiveGain * Level(i);
                return v;
            }
        }

        public double ClickValue => BaseClick * PermanentClickMultiplier * EffectMultiplier(EffectKind.ClickMultiplier);
        public double PassivePerSecond => BasePassive * PermanentPassiveMultiplier * EffectMultiplier(EffectKind.PassiveMultiplier);

        /// <summary>Produção estimada (passiva + toques de referência) usada para escalar eventos.</summary>
        public double ReferenceRate => PassivePerSecond + ClickValue * config.referenceTapsPerSecond;

        public double ClickGainPreview(int index) => config.upgrades[index].clickGain * PermanentClickMultiplier * EffectMultiplier(EffectKind.ClickMultiplier);
        public double PassiveGainPreview(int index) => config.upgrades[index].passiveGain * PermanentPassiveMultiplier * EffectMultiplier(EffectKind.PassiveMultiplier);

        // ---------- custos ----------
        public double Cost(int index)
        {
            var u = config.upgrades[index];
            double raw = u.baseCost * (1.0 - DiscountFor(index)) * Math.Pow(u.costGrowth, Level(index));
            return Math.Max(1, Math.Ceiling(raw - 1e-9));
        }

        public bool IsUnlocked(int index)
        {
            return Level(index) > 0 || S.campaignProduced >= config.upgrades[index].unlockAtProduced;
        }

        public bool CanBuy(int index) => IsUnlocked(index) && S.energy >= Cost(index);

        public bool Buy(int index)
        {
            if (!CanBuy(index)) return false;
            S.energy -= Cost(index);
            S.SetLevel(config.upgrades[index].id, Level(index) + 1);
            return true;
        }

        /// <summary>Etapa visual (0 = não instalada) segundo os marcos de nível.</summary>
        public int VisualTier(int index)
        {
            int lv = Level(index), tier = 0;
            foreach (int m in config.upgrades[index].visualMilestones) if (lv >= m) tier++;
            return tier;
        }

        public void AddEnergy(double amount)
        {
            if (amount <= 0) return;
            S.energy += amount;
            S.campaignProduced += amount;
            S.totalProducedAllTime += amount;
        }

        public bool HasAnyPassiveUpgrade()
        {
            for (int i = 0; i < config.upgrades.Count; i++)
                if (config.upgrades[i].passiveGain > 0 && Level(i) > 0) return true;
            return false;
        }

        public int TotalUpgradeLevels()
        {
            int t = 0;
            for (int i = 0; i < config.upgrades.Count; i++) t += Level(i);
            return t;
        }
    }
}
