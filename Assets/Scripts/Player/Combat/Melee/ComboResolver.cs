using System.Collections.Generic;
using UnityEngine;

namespace Player.Combat.Melee
{
    public class ComboResolver
    {
        private readonly ComboTree _tree;
        private readonly ComboInputBuffer _buffer;
        private readonly List<ComboTransition> _transitionCandidates = new List<ComboTransition>(8);

        public ComboResolver(ComboTree tree, ComboInputBuffer buffer)
        {
            _tree = tree;
            _buffer = buffer;
        }

        public bool TryResolveEntry(float now, out ComboNode node, out int consumeCount)
        {
            node = null;
            consumeCount = 0;

            if (_tree == null || _tree.entryNodes == null || !_tree.allowLocomotionConsume) return false;

            ComboNode bestNode = null;
            int bestPriority = int.MinValue;
            int bestConsumeCount = 0;

            var events = _buffer.Events;

            foreach (var entry in _tree.entryNodes)
            {
                if (entry == null || !entry.isEntry || entry.timelineAsset == null) continue;

                var input = entry.entryInput;
                if (input == null) continue;

                if (input.TryMatch(events, now, out int count))
                {
                    if (entry.entryPriority > bestPriority)
                    {
                        bestPriority = entry.entryPriority;
                        bestNode = entry;
                        bestConsumeCount = count;
                    }
                }
            }

            if (bestNode == null) return false;

            node = bestNode;
            consumeCount = bestConsumeCount;
            return true;
        }

        public bool TryResolveTransition(ComboNode currentNode, float now, out ComboTransition transition, out int consumeCount)
        {
            transition = null;
            consumeCount = 0;

            if (_tree == null || currentNode == null || currentNode.transitions == null) return false;

            var events = _buffer.Events;
            _transitionCandidates.Clear();

            foreach (var t in currentNode.transitions)
            {
                if (t == null || t.input == null || t.toNode == null) continue;

                if (t.input.TryMatch(events, now, out _))
                {
                    // TODO: 条件检查（OnGround/FacingTarget 等）
                    _transitionCandidates.Add(t);
                }
            }

            if (_transitionCandidates.Count == 0) return false;

            _transitionCandidates.Sort((a, b) => b.priority.CompareTo(a.priority));
            var best = _transitionCandidates[0];

            if (!best.input.TryMatch(events, now, out int count)) return false;

            transition = best;
            consumeCount = count;
            return true;
        }

    }
}
