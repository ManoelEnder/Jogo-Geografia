using System;
using System.Collections.Generic;

namespace NucleoVivo
{
    /// <summary>Sorteia eventos, aplica escolhas e controla efeitos temporários e reputação.</summary>
    public class EventDirector
    {
        readonly GameConfig config;
        readonly Economy economy;
        readonly Func<GameState> stateGetter;
        readonly Random rng;
        GameState S => stateGetter();

        public event Action<EventDef> EventStarted;
        public event Action<EventDef, EventChoice, bool> EventResolved; // bool = resolvido automaticamente
        public event Action EffectsChanged;

        public EventDirector(GameConfig config, Economy economy, Func<GameState> state, int seed = 0)
        {
            this.config = config;
            this.economy = economy;
            stateGetter = state;
            rng = seed == 0 ? new Random() : new Random(seed);
        }

        public EventDef Pending => S.HasPendingEvent ? config.FindEvent(S.pendingEventId) : null;

        /// <summary>Reputação com os efeitos temporários aplicados (0 a 100).</summary>
        public float EffectiveReputation
        {
            get
            {
                float r = S.reputation;
                foreach (var e in S.effects) if (e.kind == EffectKind.ReputationTemp) r += e.value;
                return Clamp(r, 0, 100);
            }
        }

        public float TimeUntilNextEvent => S.eventsEnabled && !S.HasPendingEvent ? S.eventTimer : -1f;

        public void Tick(float dt)
        {
            TickEffects(dt);

            if (!S.eventsEnabled)
            {
                if (economy.HasAnyPassiveUpgrade())
                {
                    S.eventsEnabled = true;
                    S.eventTimer = NextInterval();
                }
                return;
            }

            if (S.HasPendingEvent)
            {
                if (Pending == null) { S.pendingEventId = ""; return; } // evento removido da config
                S.pendingEventAge += dt;
                if (config.pendingEventExpire > 0 && S.pendingEventAge >= config.pendingEventExpire)
                    AutoResolve();
                return;
            }

            S.eventTimer -= dt;
            if (S.eventTimer <= 0) TriggerRandomEvent();
        }

        void TickEffects(float dt)
        {
            bool changed = false;
            for (int i = S.effects.Count - 1; i >= 0; i--)
            {
                S.effects[i].remaining -= dt;
                if (S.effects[i].remaining <= 0) { S.effects.RemoveAt(i); changed = true; }
            }
            if (changed) EffectsChanged?.Invoke();
        }

        public float NextInterval()
        {
            float t = (float)(config.eventIntervalMin + rng.NextDouble() * (config.eventIntervalMax - config.eventIntervalMin));
            // Boa reputação traz oportunidades com um pouco mais de frequência.
            float rep01 = EffectiveReputation / 100f;
            return t * Lerp(1.1f, 0.85f, rep01);
        }

        public float WeightFor(EventDef e, float reputation)
        {
            if (reputation < e.minReputation || reputation > e.maxReputation) return 0;
            if (e.choices == null || e.choices.Count == 0) return 0;
            float rep01 = reputation / 100f;
            float w = e.weight;
            switch (e.tone)
            {
                case EventTone.Positive: w *= Lerp(0.5f, 1.7f, rep01); break;
                case EventTone.Negative: w *= Lerp(1.6f, 0.6f, rep01); break;
            }
            if (e.id == S.lastEventId) w *= 0.25f; // evita repetir em sequência
            return w;
        }

        public EventDef TriggerRandomEvent()
        {
            float rep = EffectiveReputation;
            var pool = new List<EventDef>();
            var weights = new List<float>();
            float total = 0;
            foreach (var e in config.events)
            {
                float w = WeightFor(e, rep);
                if (w <= 0) continue;
                pool.Add(e); weights.Add(w); total += w;
            }
            if (pool.Count == 0) { S.eventTimer = NextInterval(); return null; }

            double roll = rng.NextDouble() * total;
            EventDef chosen = pool[pool.Count - 1];
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0) { chosen = pool[i]; break; }
            }
            StartEvent(chosen);
            return chosen;
        }

        public void StartEvent(EventDef e)
        {
            S.pendingEventId = e.id;
            S.pendingEventAge = 0;
            S.lastEventId = e.id;
            EventStarted?.Invoke(e);
        }

        public double ChoiceCost(EventChoice c)
        {
            if (c.costProductionSeconds <= 0 && c.costMin <= 0) return 0;
            return Math.Ceiling(Math.Max(c.costMin, economy.ReferenceRate * c.costProductionSeconds));
        }

        public double ChoiceReward(EventChoice c)
        {
            if (c.rewardProductionSeconds <= 0 && c.rewardMin <= 0) return 0;
            return Math.Floor(Math.Max(c.rewardMin, economy.ReferenceRate * c.rewardProductionSeconds));
        }

        public bool CanAfford(EventChoice c) => S.energy >= ChoiceCost(c);

        public bool Choose(int choiceIndex)
        {
            var e = Pending;
            if (e == null || choiceIndex < 0 || choiceIndex >= e.choices.Count) return false;
            var c = e.choices[choiceIndex];
            if (!CanAfford(c)) return false;
            Apply(e, c, false);
            return true;
        }

        void AutoResolve()
        {
            var e = Pending;
            EventChoice c = e.choices.Find(x => x.isDefaultIfIgnored && CanAfford(x));
            if (c == null) c = e.choices.Find(x => ChoiceCost(x) <= 0);
            if (c == null) { ClearPending(); return; }
            Apply(e, c, true);
        }

        void Apply(EventDef e, EventChoice c, bool automatic)
        {
            double cost = ChoiceCost(c);
            double reward = ChoiceReward(c);
            S.energy -= cost;
            if (reward > 0) economy.AddEnergy(reward);
            S.reputation = Clamp(S.reputation + c.reputationDelta, 0, 100);

            if (c.effect != EffectKind.None && c.effectDuration > 0)
            {
                // Mesmo evento repetido renova o efeito em vez de empilhar.
                S.effects.RemoveAll(x => x.sourceEventId == e.id && x.kind == c.effect);
                S.effects.Add(new ActiveEffect
                {
                    sourceEventId = e.id,
                    label = e.title,
                    kind = c.effect,
                    value = c.effectValue,
                    duration = c.effectDuration,
                    remaining = c.effectDuration
                });
                EffectsChanged?.Invoke();
            }

            S.eventsResolved++;
            ClearPending();
            EventResolved?.Invoke(e, c, automatic);
        }

        void ClearPending()
        {
            S.pendingEventId = "";
            S.pendingEventAge = 0;
            S.eventTimer = NextInterval();
        }

        /// <summary>Texto curto do efeito de uma escolha, mostrado antes de confirmar.</summary>
        public string DescribeChoice(EventChoice c)
        {
            var parts = new List<string>();
            double cost = ChoiceCost(c);
            double reward = ChoiceReward(c);
            if (cost > 0) parts.Add("Custo: " + NumberFormat.Short(cost) + " de Energia");
            if (reward > 0) parts.Add("Ganho: +" + NumberFormat.Short(reward) + " de Energia");
            if (c.reputationDelta != 0) parts.Add("Reputação " + (c.reputationDelta > 0 ? "+" : "") + c.reputationDelta);
            if (c.effect != EffectKind.None && c.effectDuration > 0)
                parts.Add(DescribeEffect(c.effect, c.effectValue) + " por " + NumberFormat.Duration(c.effectDuration));
            if (parts.Count == 0) parts.Add("Sem custo e sem efeito");
            return string.Join("  •  ", parts);
        }

        public static string DescribeEffect(EffectKind kind, float value)
        {
            switch (kind)
            {
                case EffectKind.PassiveMultiplier: return "Produção passiva " + Signed(value - 1f);
                case EffectKind.ClickMultiplier: return "Energia por toque " + Signed(value - 1f);
                case EffectKind.ReputationTemp: return "Reputação " + (value > 0 ? "+" : "") + value.ToString("0");
                default: return "";
            }
        }

        static string Signed(float fraction) => (fraction >= 0 ? "+" : "−") + NumberFormat.Percent(Math.Abs(fraction));

        static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
        static float Lerp(float a, float b, float t) => a + (b - a) * Clamp(t, 0, 1);
    }
}
