using UnityEngine;

namespace ZomCity
{
    [CreateAssetMenu(menuName = "ZomCity/Camera/Camera Profile", fileName = "ZomCityCameraProfile")]
    public sealed class ZomCityCameraProfile : ScriptableObject
    {
        public ZomCityCameraStateDefinition Explore = ZomCityCameraStateDefinition.CreatePreset(ZomCityCameraState.Explore);
        public ZomCityCameraStateDefinition Aim = ZomCityCameraStateDefinition.CreatePreset(ZomCityCameraState.Aim);
        public ZomCityCameraStateDefinition Sprint = ZomCityCameraStateDefinition.CreatePreset(ZomCityCameraState.Sprint);
        public ZomCityCameraStateDefinition Dead = ZomCityCameraStateDefinition.CreatePreset(ZomCityCameraState.Dead);
        public ZomCityCameraStateDefinition DragZoomFar = new ZomCityCameraStateDefinition
        {
            ProjectionMode = ZomCityCameraComposer.CameraProjectionMode.DioramaPerspective,
            FieldOfView = 34f,
            PitchDown = 10f,
            Yaw = 0f,
            Distance = 18f,
            FramingAnchorX = 0.38f,
            FramingAnchorY = 0.46f,
            Smoothness = 0.13f,
            EnablePixelSnapInOrthographicLegacy = true,
        };

        public ZomCityCameraStateDefinition Get(ZomCityCameraState state)
        {
            switch (state)
            {
                case ZomCityCameraState.Aim:
                    return Aim;
                case ZomCityCameraState.Sprint:
                    return Sprint;
                case ZomCityCameraState.Dead:
                    return Dead;
                default:
                    return Explore;
            }
        }

        public void Set(ZomCityCameraState state, ZomCityCameraStateDefinition definition)
        {
            switch (state)
            {
                case ZomCityCameraState.Aim:
                    Aim = definition;
                    break;
                case ZomCityCameraState.Sprint:
                    Sprint = definition;
                    break;
                case ZomCityCameraState.Dead:
                    Dead = definition;
                    break;
                default:
                    Explore = definition;
                    break;
            }
        }
    }
}
