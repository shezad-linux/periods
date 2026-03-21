using UnityEngine;

namespace Lumenfall.Data
{
    public interface IContextActionTarget
    {
        ContextActionType ActionType { get; }

        int Priority { get; }

        Transform TargetTransform { get; }

        bool IsAvailable(SaveGameData saveData);

        void Perform(GameObject actor);
    }
}
