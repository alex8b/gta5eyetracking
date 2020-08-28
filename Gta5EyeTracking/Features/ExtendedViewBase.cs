//-----------------------------------------------------------------------
// Copyright 2016 Tobii AB (publ). All rights reserved.
//-----------------------------------------------------------------------

using System;
using Gta5EyeTracking;
using GTA;
using SharpDX;

/*
 * Extended View
 * 
 * Extended view decouples your look direction from your movement direction. 
 * This lets you both look around a bit more without conciously have to move 
 * the mouse, but also lets you inspect objects that are locked to the camera's 
 * reference frame.
 * 
 * This implementation works in an absolute reference frame, essentially 
 * mapping each possible gaze point directly to a camera target orientation and
 * then interpolating the camera to that orientation. It does this using two 
 * levels of indirection, in this order:  Gaze point -> View target -> Camera 
 * rotation. The main reason is to smooth out the action when the camera 
 * rotation is close to the gaze point, but having the intermediate view target
 * is also useful when we want to pause the system, letting it smoothly come to
 * a stop instead of instantly halting it.
 */
public abstract class ExtendedViewBase
{
    private const float HeadPoseRotationalRangeRadians = Mathf.PI;
    private const float ReferenceFrequency = 60;
    private const float HeadTrackingFrequency = 30;

    public bool IsUsingHeadPose = true;

    public ExtendedViewType InfiniteScreenExtendedViewType = ExtendedViewType.Direct;


    /* This is an additional scale value complementing Sensitivity, but is only 
	* applied when lerping the camera towards the view target. */

    public float GazeViewResponsiveness = 0.25f;
    public float GazeViewSensitivityScale = 0.15f;
    public float GazeViewMinimumExtensionAngleDegrees = 20f;

    public float GazeSensitivityExponent = 2.25f;
    public float GazeSensitivityInflectionPoint = 0.8f;
    public float GazeSensitivityStartPoint = 0;
    public float GazeSensitivityEndPoint = 1;


    public float HeadViewResponsiveness = 1.0f;
    public float HeadViewSensitivityScale = 0.65f;

    public float HeadSensitivityExponent = 1.25f;
    public float HeadSensitivityInflectionPoint = 0.5f;
    public float HeadSensitivityStartPoint = 0;
    public float HeadSensitivityEndPoint = 1;

    public float GazeViewResetResponsiveness = 5;
    public float HeadViewResetResponsiveness = 5;

    //-------------------------------------------------------------------------
    // Public members
    //-------------------------------------------------------------------------
    //#if (UNITY_5 || UNITY_4_6_OR_NEWER)
    //	/* If you are at an extreme view angle and quickly want to return to the center, 
    //	 * it is nice to be able to look at a given stimulus (usually the crosshair).*/
    //	public Image OptionalReturnToCenterStimuli;
    //#endif
    /* Should we remove extended view when aiming to allow more precise cursor 
	 * movements? */
    public bool RemoveExtendedViewWhenAiming = true;
    /* Should we pause the system when the user is looking at UI? */
    public bool PauseInCleanUiDeadzones = true;
    /* We want to make sure to completely center the view when the user is 
	 * looking straight forward. We also want to speed up the centering process if
	 * the user is at an extreme view angle and wants back to the middle. */
    public float ViewCenteringDeadzoneSize = 0.1f;

    /* This lets you scale the amount of Infinite Screen effect depending on how 
	 * much change in pitch your have. This might be useful since we get strange 
	 * effects when using Infinite Screen in extreme pitch angles. */
    //public AnimationCurve AmountOfScreenShiftDependingOnCameraPitch = AnimationCurve.EaseInOut(0.0f, 1.0f, 1.0f, 1.0f);
    public bool ScaleScreenShiftByBasePitch = false;
    public float AimTransitionLerpScalar = 10f;
    public bool IsEnabled = true;
    public bool IsLocked = false;

    public bool IsAiming { get; set; }

    public bool IsPaused { get { return false; /*Time.deltaTime < 1E-08f;*/ } }

    /* The extra yaw produced by the system */
    public float YawOffset { get; protected set; }

    /* The extra pitch produced by the system */
    public float PitchOffset { get; protected set; }

    /* The extra yaw produced by the system */
    public float HeadYawOffset { get; protected set; }

    /* The extra pitch produced by the system */
    public float HeadPitchOffset { get; protected set; }

    public float Pitch
    {
        get { return PitchOffset + (HeadPitchOffset * HeadRotationScalar); }
    }

    public float Yaw
    {
        get { return YawOffset + (HeadYawOffset * HeadRotationScalar); }
    }

    /* Head position data */

    public float HeadRotationScalar;

    //-------------------------------------------------------------------------
    // Protected members
    //-------------------------------------------------------------------------

    protected Vector2 AimTargetScreen;
    protected Ray AimTargetRay;


    //-------------------------------------------------------------------------
    // Private members
    //-------------------------------------------------------------------------

    /* This is an additional scale value complementing Sensitivity, but is only 
	 * applied when lerping the camera towards the view target. */


    private Camera _cameraWithoutExtendedView;
    private Camera _cameraWithExtendedView;

    private bool _lastIsAiming;

    /* This variable are used for main extended view algorithm */
    private Vector3 _currentHeadPose = new Vector3(0, 0, 0);
    private float _gazeViewExtensionAngleDegrees;

    private Vector3 _filteredHeadPose;
    private Vector3 _lerpedFilteredHeadPose;

    private Vector2 _lastValidGazePoint;
    private Vector2 _gazeViewTarget;


    private long _lastHeadPosePreciseTimestamp = Int64.MinValue;
    private float _accumulatedTimeDeltaForHeadPoseLerp;
    private Vector3 _previousFilteredHeadPose;


    //-------------------------------------------------------------------------
    // Public properties
    //-------------------------------------------------------------------------

    //	public virtual Camera CameraWithoutExtendedView
    //	{
    //		get
    //		{
    //			if (_cameraWithoutExtendedView != null && _cameraWithoutExtendedView.gameObject != null) return _cameraWithoutExtendedView;

    //			var cameraGo = new GameObject("CameraTransformWithoutExtendedView");
    //#if ((UNITY_5 || UNITY_4_6_OR_NEWER))
    //            cameraGo.transform.SetParent(null);
    //#else
    //            cameraGo.transform.parent = null;
    //#endif
    //            _cameraWithoutExtendedView = cameraGo.AddComponent<Camera>();
    //			_cameraWithoutExtendedView.enabled = false;
    //			return _cameraWithoutExtendedView;
    //		}
    //	}


    //	public virtual Camera CameraWithExtendedView
    //	{
    //		get
    //		{
    //			if (_cameraWithExtendedView != null && _cameraWithExtendedView.gameObject != null) return _cameraWithExtendedView;

    //			var cameraGo = new GameObject("CameraTransformWithExtendedView");
    //#if ((UNITY_5 || UNITY_4_6_OR_NEWER))
    //            cameraGo.transform.SetParent(null);
    //#else
    //            cameraGo.transform.parent = null;
    //#endif
    //            _cameraWithExtendedView = cameraGo.AddComponent<Camera>();
    //			_cameraWithExtendedView.enabled = false;
    //			return _cameraWithExtendedView;
    //		}
    //	}

    protected virtual void UpdateSettings()
    {
    }

    protected virtual void UpdateTransform()
    {
    }

    //--------------------------------------------------------------------
    // MonoBehaviour event functions (messages)
    //--------------------------------------------------------------------
    protected void LateUpdate()
    {
        UpdateSettings();

        UpdateHeadPose();

        if (!IsEnabled/* || !TobiiAPI.GetUserPresence().IsUserPresent*/
            || (IsAiming && RemoveExtendedViewWhenAiming))
        {
            _gazeViewTarget = new Vector2();
        }
        else
        {
            Vector2 currentGazePoint = TobiiAPI.GetGazePoint();
            //if (currentGazePoint.IsValid)
            {
                _lastValidGazePoint = currentGazePoint;
            }

            if (!ShouldPauseInfiniteScreenOnCleanUI())
            {
                //if (_lastValidGazePoint.IsValid)
                {
                    var normalizedCenteredGazeCoordinates = CreateNormalizedCenteredGazeCoordinates(_lastValidGazePoint);
                    if (!IsLocked)
                    {
                        UpdateViewTarget(normalizedCenteredGazeCoordinates);
                    }
                }
            }
        }

        UpdateInfiniteScreenAngles();
        UpdateTransform();
    }

    private void UpdateHeadPose()
    {
        if (!IsEnabled)
        {
            HeadRotationScalar = Mathf.Lerp(HeadRotationScalar, 0, Time.unscaledDeltaTime * HeadViewResetResponsiveness);
        }
        else
        {
            HeadRotationScalar = !IsAiming ? Mathf.Lerp(HeadRotationScalar, 1, Time.unscaledDeltaTime * HeadViewResetResponsiveness) : 0;
        }

        var headPose = TobiiAPI.GetHeadPose();
        var currentFilterredHeadPose = _filteredHeadPose;
        if (headPose.TimeStampMicroSeconds > _lastHeadPosePreciseTimestamp)
        {
            _lastHeadPosePreciseTimestamp = headPose.TimeStampMicroSeconds;
            _currentHeadPose = new Vector3(headPose.Rotation.Pitch, headPose.Rotation.Yaw, headPose.Rotation.Roll);
            UpdateFilteredHeadPose(_currentHeadPose, Time.unscaledDeltaTime);
            _previousFilteredHeadPose = currentFilterredHeadPose;
            _accumulatedTimeDeltaForHeadPoseLerp -= (1.0f / HeadTrackingFrequency);
        }

        _accumulatedTimeDeltaForHeadPoseLerp += Time.unscaledDeltaTime;
        _accumulatedTimeDeltaForHeadPoseLerp = Mathf.Clamp(_accumulatedTimeDeltaForHeadPoseLerp, 0.0f, 1.0f / HeadTrackingFrequency);

        var lerpStep = _accumulatedTimeDeltaForHeadPoseLerp * HeadTrackingFrequency;

        _lerpedFilteredHeadPose.X = Mathf.Lerp(_previousFilteredHeadPose.X, _filteredHeadPose.X, lerpStep);
        _lerpedFilteredHeadPose.Y = Mathf.Lerp(_previousFilteredHeadPose.Y, _filteredHeadPose.Y, lerpStep);

        var centeredNormalizedHeadPose = CreateCenterNormalizedHeadPose(_lerpedFilteredHeadPose);

        var headViewAngles = GetHeadViewAngles(centeredNormalizedHeadPose);

        HeadYawOffset = -headViewAngles.Y;
        HeadPitchOffset = -headViewAngles.X * 1.5f;
    }

    //-------------------------------------------------------------------------
    // Protected/public virtual functions
    //-------------------------------------------------------------------------

    // TODO - why public? why not protected?
    public virtual void AimAtWorldPosition(Vector3 worldPosition)
    {
        /* empty default implementation */
    }

    protected virtual bool ShouldPauseInfiniteScreenOnCleanUI()
    {
        return false;
    }

    //-------------------------------------------------------------------------
    // Protected functions
    //-------------------------------------------------------------------------

    //protected void ProcessAimAtGaze(Camera mainCamera)
    //{
    //	if (!_lastIsAiming && IsAiming && TobiiAPI.GetGazePoint().IsValid)
    //	{
    //		AimTargetScreen = TobiiAPI.GetGazePoint().Screen;
    //		var aimTargetWorld = mainCamera.ScreenToWorldPoint(new Vector3(AimTargetScreen.x, AimTargetScreen.y, 10));
    //		AimTargetRay = mainCamera.ScreenPointToRay(new Vector3(AimTargetScreen.x, AimTargetScreen.y, 10));

    //		AimAtWorldPosition(aimTargetWorld);
    //	}

    //	_lastIsAiming = IsAiming;
    //}

    //protected void UpdateCameraWithoutExtendedView(Camera mainCamera)
    //{
    //       UpdateCamera(CameraWithoutExtendedView, mainCamera);
    //}

    //protected void UpdateCameraWithExtendedView(Camera mainCamera)
    //{
    //       UpdateCamera(CameraWithExtendedView, mainCamera);
    //}

    //   protected void UpdateCamera(Camera camera, Camera mainCamera)
    //   {
    //       camera.transform.position = mainCamera.transform.position;
    //       camera.transform.rotation = mainCamera.transform.rotation;
    //       camera.fieldOfView = mainCamera.fieldOfView;
    //   }

    //protected void Rotate(Component componentToRotate, float fovScalar = 1f, Vector3 up = new Vector3())
    //{
    //       Transform transformToRotate = componentToRotate is Transform ? componentToRotate as Transform : componentToRotate.transform;

    //       transformToRotate.Rotate(Pitch * fovScalar, 0.0f, 0.0f, Space.Self);
    //	if (up == new Vector3())
    //	{
    //		transformToRotate.Rotate(0.0f, Yaw * fovScalar, 0.0f, Space.World);
    //	}
    //	else
    //	{
    //		transformToRotate.Rotate(up, Yaw * fovScalar, Space.World);
    //	}
    //}

    //   protected void RotateAndGetCrosshairScreenPosition(Camera camera, out Vector2 crosshairScreenPosition, float fovScalar = 1f, Vector3 up = new Vector3())
    //   {
    //       Vector3 vector;
    //       RotateAndGetCrosshairScreenPosition(camera, out vector, fovScalar, up);
    //       crosshairScreenPosition = vector;
    //   }

    //   protected void RotateAndGetCrosshairScreenPosition(Camera camera, out Vector3 crosshairScreenPosition, float fovScalar = 1f, Vector3 up = new Vector3())
    //   {
    //       var crosshairWorldPosition = camera.transform.position + camera.transform.forward;
    //       Rotate(camera.transform, fovScalar, up);
    //       crosshairScreenPosition = camera.WorldToScreenPoint(crosshairWorldPosition);
    //   }

    //   protected void RotateAndGetCrosshairViewportPosition(Camera camera, out Vector2 crosshairScreenPosition, float fovScalar = 1f, Vector3 up = new Vector3())
    //   {
    //       Vector3 vector;
    //       RotateAndGetCrosshairViewportPosition(camera, out vector, fovScalar, up);
    //       crosshairScreenPosition = vector;
    //   }

    //   protected void RotateAndGetCrosshairViewportPosition(Camera camera, out Vector3 crosshairScreenPosition, float fovScalar = 1f, Vector3 up = new Vector3())
    //   {
    //       var crosshairWorldPosition = camera.transform.position + camera.transform.forward;
    //       Rotate(camera.transform, fovScalar, up);
    //       crosshairScreenPosition = camera.WorldToViewportPoint(crosshairWorldPosition);
    //   }

    //   protected IEnumerator ResetCameraWorld(Quaternion rotation, Transform transform = null)
    //   {
    //       transform = transform ? transform : this.transform;

    //       yield return new WaitForEndOfFrame();
    //       transform.rotation = rotation;
    //       PostResetCamera();
    //   }

    //   protected IEnumerator ResetCameraLocal(Quaternion? rotation = null, Transform transform = null)
    //   {
    //       transform = transform ? transform : this.transform;
    //       rotation = rotation.HasValue ? rotation : Quaternion.identity;

    //       yield return new WaitForEndOfFrame();
    //       transform.localRotation = rotation.Value;
    //       PostResetCamera();
    //   }

    protected virtual void PostResetCamera()
    {

    }

    /// <summary>
    /// Lerps the view target closer to the gaze point.
    /// </summary>
    private void UpdateViewTarget(Vector2 normalizedCenteredGazeCoordinates)
    {
        var responsiveness = Mathf.Pow(GazeViewResponsiveness, 2.5f);
        var scale = Mathf.Clamp01(Time.unscaledDeltaTime * ReferenceFrequency);
        _gazeViewTarget.X = Mathf.Lerp(_gazeViewTarget.X, normalizedCenteredGazeCoordinates.X, responsiveness * scale);
        _gazeViewTarget.Y = Mathf.Lerp(_gazeViewTarget.Y, normalizedCenteredGazeCoordinates.Y, responsiveness * scale);

        /* If you are at an extreme view angle and quickly want to return to the center, 
		 * it is nice to be able to look at a given stimulus (usually the crosshair). */
        //TODO
        //if (userLookingInCenteringDeadzone)
        //{
        //	_gazeViewTarget.x = Mathf.Lerp(_gazeViewTarget.x, 0.0f, 10 * Time.unscaledDeltaTime);
        //	_gazeViewTarget.y = Mathf.Lerp(_gazeViewTarget.y, 0.0f, 10 * Time.unscaledDeltaTime);
        //}
    }

    //private void OnGUI()
    //{
    //	GUI.backgroundColor = Color.blue;
    //	GUI.Box(new Rect((_gazeViewTarget.x + 1) * 0.5f * Screen.width - 5, Screen.height - ((_gazeViewTarget.y + 1) * 0.5f * Screen.height) - 5, 10, 10), " ");
    //}

    /// <summary>
    /// Translates the current view target to target orientation angles and lerp 
    /// the camera orientation towards it.
    /// </summary>
    private void UpdateInfiniteScreenAngles()
    {
        //Update angles
        var displayAspectRatio = TobiiAPI.AspectRatio;

        _gazeViewExtensionAngleDegrees = GetGazeViewExtensionAngleDegrees(_lerpedFilteredHeadPose);

        /* Translate gaze offset to angles along our curve */

        //Boost gaze angles for IS-3 units
        float gazeViewExtensionAngleDegrees = IsUsingHeadPose
            ? _gazeViewExtensionAngleDegrees
            : (_gazeViewExtensionAngleDegrees * 1.25f);


        /* Translate gaze offset to angles along our curve */
        var yawLimit = gazeViewExtensionAngleDegrees * displayAspectRatio;
        float targetYaw = _gazeViewTarget.X * yawLimit;

        var pitchLimit = gazeViewExtensionAngleDegrees;
        float targetPitch = _gazeViewTarget.Y * pitchLimit;

        //if (ScaleScreenShiftByBasePitch)
        //{
        //	float cameraPitchWithin90Range = transform.localRotation.eulerAngles.x > 90
        //		? transform.localRotation.eulerAngles.x - 360
        //		: transform.localRotation.eulerAngles.x;
        //	float pitchShiftMinus1To1 = cameraPitchWithin90Range/90.0f;
        //	float pitchShift01 = (pitchShiftMinus1To1 + 1)/2.0f;
        //	float pitchShiftScale = AmountOfScreenShiftDependingOnCameraPitch.Evaluate(pitchShift01);
        //	targetYaw *= pitchShiftScale;
        //}

        /* Rotate current angles toward our target angles.
		 * Please note that depending on preference, a slerp here might be a better 
		 * fit because of angle spacing errors when using lerp with angles. */

        var deltaVector = new Vector2((targetYaw - YawOffset) / yawLimit, (targetPitch - PitchOffset) / pitchLimit);
        var normalizedDistance = Mathf.Clamp01(deltaVector.Length());
        var viewTargetStepSize = TobiiSensitivityGradient(normalizedDistance, GazeSensitivityExponent,
            GazeSensitivityInflectionPoint, GazeSensitivityStartPoint, GazeSensitivityEndPoint);


        YawOffset = Mathf.LerpAngle(YawOffset, targetYaw,
            (IsAiming || !IsEnabled ? GazeViewResetResponsiveness : viewTargetStepSize) * Time.unscaledDeltaTime);
        PitchOffset = Mathf.LerpAngle(PitchOffset, targetPitch,
            (IsAiming || !IsEnabled ? GazeViewResetResponsiveness : viewTargetStepSize) * Time.unscaledDeltaTime);
    }

    //-------------------------------------------------------------------------
    // Static utility functions
    //-------------------------------------------------------------------------

    /// <summary>
    /// Creates normalized centered gaze coordinates from the provided Gaze Point.
    /// Centered normalized means bottom left is (-1,-1) and top right is (1,1).
    /// </summary>
    /// <param name="gazePoint">Gaze point</param>
    /// <returns>Normalized centered gaze coordinates if the gaze point is not 
    /// null, otherwise a zero-valued Vector2.</returns>
    private static Vector2 CreateNormalizedCenteredGazeCoordinates(Vector2 gazePoint)
    {
        var normalizedCenteredGazeCoordinates = (gazePoint - new Vector2(0.5f, 0.5f)) * 2;
        //normalizedCenteredGazeCoordinates.y = -normalizedCenteredGazeCoordinates.y;

        normalizedCenteredGazeCoordinates.X = Mathf.Clamp(normalizedCenteredGazeCoordinates.X, -1.0f, 1.0f);
        normalizedCenteredGazeCoordinates.Y = Mathf.Clamp(normalizedCenteredGazeCoordinates.Y, -1.0f, 1.0f);
        return normalizedCenteredGazeCoordinates;
    }

    private static Vector3 CreateCenterNormalizedHeadPose(Vector3 headRotation)
    {
        var halfHeadPoseRotationalRangeRadians = HeadPoseRotationalRangeRadians / 2.0f;

        return headRotation / halfHeadPoseRotationalRangeRadians;
    }

    private void UpdateFilteredHeadPose(Vector3 headPose, float timeDeltaInSeconds)
    {
        float responsiveNess = Mathf.Pow(HeadViewResponsiveness, 2.5f);
        float timeDeltaFactor = Mathf.Clamp01(timeDeltaInSeconds * ReferenceFrequency);

        float halfHeadPoseRotationalRangeRadians = HeadPoseRotationalRangeRadians / 2.0f;

        _filteredHeadPose.Y += DampenDelta(DampenHeadPose, headPose.Y - _filteredHeadPose.Y, -halfHeadPoseRotationalRangeRadians, halfHeadPoseRotationalRangeRadians) * responsiveNess * timeDeltaFactor;
        _filteredHeadPose.X += DampenDelta(DampenHeadPose, headPose.X - _filteredHeadPose.X, -halfHeadPoseRotationalRangeRadians, halfHeadPoseRotationalRangeRadians) * responsiveNess * timeDeltaFactor;
        _filteredHeadPose.Z += DampenDelta(DampenHeadPose, headPose.Z - _filteredHeadPose.Z, -halfHeadPoseRotationalRangeRadians, halfHeadPoseRotationalRangeRadians) * responsiveNess * timeDeltaFactor;
    }

    private static float DampenDelta(Func<float, float> dampeningFunction, float delta, float minValue, float maxValue)
    {
        float valueRangeLength = maxValue - minValue;
        float deltaSign = Mathf.Sign(delta);
        float normalizedDelta = Mathf.Clamp01(Mathf.Abs(delta) / valueRangeLength);
        float transformedDelta = deltaSign * dampeningFunction(normalizedDelta) * valueRangeLength;
        return transformedDelta;
    }

    private static float DampenHeadPose(float value)
    {
        return Mathf.Pow(value, 1.75f);
    }

    private static float TobiiSensitivityGradient(float normalizedValue, float exponent, float inflectionPoint, float startPoint, float endPoint)
    {
        if (startPoint >= 1.0f)
        {
            return 0.0f;
        }

        if (endPoint <= 0.0f)
        {
            return 1.0f;
        }

        exponent = Mathf.Max(exponent, 1.0f);
        inflectionPoint = Mathf.Clamp(inflectionPoint, float.Epsilon, (1.0f - float.Epsilon));

        float x = (normalizedValue - startPoint) / (endPoint - startPoint);
        x = Mathf.Clamp01(x);

        float a = 1.0f / inflectionPoint;
        float b = a * x;
        float c = a / (a - 1.0f);
        float d = Mathf.Min(Mathf.Floor(b), 1.0f);

        return ((1.0f - d) * (Mathf.Pow(b, exponent) / a)) + (d * (1.0f - (Mathf.Pow(c * (1.0f - x), exponent) / c)));
    }

    private static float GetSensititivyGradientValue(float centerNormalizedValue, float exponent, float inflectionPoint, float startPoint, float endPoint)
    {
        var sign = Mathf.Sign(centerNormalizedValue);
        return sign * TobiiSensitivityGradient(Mathf.Clamp01(Mathf.Abs(centerNormalizedValue)), exponent, inflectionPoint, startPoint, endPoint);
    }

    private float GetGazeViewExtensionAngleDegrees(Vector3 centerNormalizedHeadPose)
    {
        switch (InfiniteScreenExtendedViewType)
        {
            case ExtendedViewType.Direct:
                {
                    return GazeViewMinimumExtensionAngleDegrees;
                }

            case ExtendedViewType.Dynamic:
                {
                    var headPoseLength = Mathf.Clamp01(Mathf.Sqrt(centerNormalizedHeadPose.Y * centerNormalizedHeadPose.Y + centerNormalizedHeadPose.X * centerNormalizedHeadPose.X));
                    return GazeViewMinimumExtensionAngleDegrees + (headPoseLength * 90.0f);
                }

            case ExtendedViewType.None:
            default:
                return 0.0f;
        }
    }

    private Vector3 GetHeadViewAngles(Vector3 centerNormalizedHeadPose)
    {
        var angles = new Vector3();

        angles.Y = GetSensititivyGradientValue(centerNormalizedHeadPose.Y, HeadSensitivityExponent, HeadSensitivityInflectionPoint, HeadSensitivityStartPoint, HeadSensitivityEndPoint) * HeadViewSensitivityScale * 180.0f;
        angles.X = GetSensititivyGradientValue(centerNormalizedHeadPose.X, HeadSensitivityExponent, HeadSensitivityInflectionPoint, HeadSensitivityStartPoint, HeadSensitivityEndPoint) * HeadViewSensitivityScale * 180.0f;

        return angles;
    }
}

public enum ExtendedViewType
{
    Direct,
    Dynamic,
    None
}
