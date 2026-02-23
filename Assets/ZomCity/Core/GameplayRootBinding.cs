using UnityEngine;

namespace ZomCity
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ZomCity/Gameplay Root Binding")]
    public sealed class GameplayRootBinding : MonoBehaviour
    {
        public Transform PhysicsRoot;
        public Transform VisualRoot;
        public string ActorId;
        public bool AutoBindChildByName = true;

        public Transform GetPhysicsRoot()
        {
            if (PhysicsRoot != null)
            {
                return PhysicsRoot;
            }

            if (AutoBindChildByName)
            {
                var found = transform.Find("PhysicsRoot");
                if (found != null)
                {
                    PhysicsRoot = found;
                    return found;
                }
            }

            return transform;
        }

        public string ResolveActorId()
        {
            if (string.IsNullOrWhiteSpace(ActorId))
            {
                return gameObject.name;
            }

            return ActorId;
        }

        private void Reset()
        {
            TryAutoBind();
        }

        private void OnValidate()
        {
            TryAutoBind();
        }

        private void OnEnable()
        {
            GameplayPlaneConstraint.Register(GetPhysicsRoot(), ResolveActorId());
        }

        private void OnDisable()
        {
            GameplayPlaneConstraint.Unregister(GetPhysicsRoot());
        }

        private void TryAutoBind()
        {
            if (!AutoBindChildByName)
            {
                return;
            }

            if (PhysicsRoot == null)
            {
                var physics = transform.Find("PhysicsRoot");
                if (physics != null)
                {
                    PhysicsRoot = physics;
                }
            }

            if (VisualRoot == null)
            {
                var visual = transform.Find("VisualRoot");
                if (visual != null)
                {
                    VisualRoot = visual;
                }
            }
        }
    }
}
