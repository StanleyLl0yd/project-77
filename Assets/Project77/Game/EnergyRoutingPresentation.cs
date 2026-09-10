using System;
using System.Collections.Generic;
using System.Text;

namespace Project77.Game
{
    public static class EnergyRoutingPresentation
    {
        public static string PairCode(string pairId)
        {
            if (string.IsNullOrWhiteSpace(pairId))
            {
                return "?";
            }

            switch (pairId)
            {
                case "red": return "R";
                case "blue": return "B";
                case "green": return "G";
                case "yellow": return "Y";
                default: return pairId.Substring(0, 1).ToUpperInvariant();
            }
        }

        public static string EndpointLabel(string pairId, int endpointNumber)
        {
            if (endpointNumber != 1 && endpointNumber != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(endpointNumber));
            }

            return PairCode(pairId) + endpointNumber;
        }

        public static string Instruction(int levelNumber, int blockedCount, int connectedPairCount)
        {
            if (levelNumber <= 1 && connectedPairCount == 0)
            {
                return "HOW TO PLAY: touch R1, keep your finger down, and drag to R2. Move through neighboring squares — up, down, left or right.";
            }

            if (levelNumber <= 2)
            {
                return "GOAL: connect every matching label, such as R1 to R2. Keep your finger down while dragging. Paths cannot share squares.";
            }

            if (blockedCount > 0)
            {
                return "GOAL: connect every matching label. X means blocked — route around it. Paths cannot cross or share squares.";
            }

            return "GOAL: connect every matching label. Drag up, down, left or right; paths cannot cross or share squares.";
        }

        public static string Legend(IEnumerable<string> pairIds, bool hasBlockedCells)
        {
            if (pairIds == null)
            {
                throw new ArgumentNullException(nameof(pairIds));
            }

            var builder = new StringBuilder("MATCH: ");
            var first = true;
            foreach (var pairId in pairIds)
            {
                if (!first)
                {
                    builder.Append("   ");
                }

                var code = PairCode(pairId);
                builder.Append(code).Append("1 <-> ").Append(code).Append('2');
                first = false;
            }

            if (first)
            {
                builder.Append("none");
            }

            builder.Append(hasBlockedCells ? "   |   X = BLOCKED" : "   |   No blocked squares");
            return builder.ToString();
        }
    }
}
