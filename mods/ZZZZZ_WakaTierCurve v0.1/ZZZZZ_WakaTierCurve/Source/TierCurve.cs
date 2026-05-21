using System.Collections.Generic;

namespace WakaTierCurve
{
    /// <summary>
    /// GameStage-indexed tier weight curve. Each "anchor" defines weights for
    /// tiers 1..15 at a specific GS; weights are linearly interpolated between
    /// neighboring anchors. SampleTier draws one tier from the interpolated
    /// distribution.
    /// </summary>
    internal static class TierCurve
    {
        // tier 1..15 indexed weights at each anchor. Phase 2: hardcoded.
        private struct Anchor
        {
            public int Gs;
            public double[] W; // length 15, index 0 = tier 1
            public Anchor(int gs, double[] w) { Gs = gs; W = w; }
        }

        private static readonly Anchor[] Anchors = new[]
        {
            //              T1    T2    T3    T4    T5    T6    T7    T8    T9    T10   T11   T12   T13   T14   T15
            new Anchor(  0, new[]{ 1.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00 }),
            new Anchor( 30, new[]{ 0.70, 0.30, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00 }),
            new Anchor( 60, new[]{ 0.40, 0.40, 0.20, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00 }),
            new Anchor(100, new[]{ 0.20, 0.30, 0.30, 0.15, 0.05, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00 }),
            new Anchor(150, new[]{ 0.05, 0.15, 0.25, 0.25, 0.20, 0.10, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00 }),
            new Anchor(250, new[]{ 0.00, 0.05, 0.15, 0.20, 0.20, 0.20, 0.15, 0.05, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00 }),
            new Anchor(400, new[]{ 0.00, 0.00, 0.05, 0.10, 0.15, 0.20, 0.20, 0.15, 0.10, 0.05, 0.00, 0.00, 0.00, 0.00, 0.00 }),
            new Anchor(800, new[]{ 0.00, 0.00, 0.00, 0.00, 0.00, 0.10, 0.15, 0.20, 0.20, 0.15, 0.10, 0.05, 0.05, 0.00, 0.00 }),
        };

        /// <summary>
        /// Sample a tier from the interpolated weight distribution at the given
        /// GameStage. Returns 0 if no tier has any weight (shouldn't happen for
        /// gs >= 0 with current anchors).
        /// </summary>
        public static int SampleTier(int gs, GameRandom rng, IReadOnlyDictionary<int, int> availableTiers)
        {
            var weights = InterpolateWeights(gs);

            // Filter to only tiers that this baseName actually has defined.
            // This keeps the curve "shape" but avoids picking tiers that won't resolve.
            double total = 0.0;
            for (int t = 1; t <= 15; t++)
            {
                if (availableTiers == null || availableTiers.ContainsKey(t))
                    total += weights[t - 1];
                else
                    weights[t - 1] = 0.0;
            }
            if (total <= 0.0) return 0;

            double roll = rng.RandomFloat * total;
            double acc = 0.0;
            for (int t = 1; t <= 15; t++)
            {
                acc += weights[t - 1];
                if (roll <= acc) return t;
            }
            return 15;
        }

        private static double[] InterpolateWeights(int gs)
        {
            var result = new double[15];

            if (gs <= Anchors[0].Gs)
            {
                System.Array.Copy(Anchors[0].W, result, 15);
                return result;
            }

            int lastIdx = Anchors.Length - 1;
            if (gs >= Anchors[lastIdx].Gs)
            {
                System.Array.Copy(Anchors[lastIdx].W, result, 15);
                return result;
            }

            // Find bracketing anchors.
            for (int i = 0; i < lastIdx; i++)
            {
                var a = Anchors[i];
                var b = Anchors[i + 1];
                if (gs < b.Gs)
                {
                    double t = (double)(gs - a.Gs) / (b.Gs - a.Gs);
                    for (int k = 0; k < 15; k++)
                    {
                        result[k] = a.W[k] + (b.W[k] - a.W[k]) * t;
                    }
                    return result;
                }
            }

            // Fallthrough (shouldn't hit).
            System.Array.Copy(Anchors[lastIdx].W, result, 15);
            return result;
        }
    }
}
