//-----------------------------------------------------------------------
// <copyright file="ARViewManager.cs" company="Google LLC">
//
// Copyright 2020 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace Google.XR.ARCoreExtensions.Samples.PersistentCloudAnchors
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    using UnityEngine.XR.ARFoundation;
    using UnityEngine.XR.ARSubsystems;

    /// <summary>
    /// A manager component that helps with hosting and resolving Cloud Anchors.
    /// </summary>
    public class ARViewManager : MonoBehaviour
    {
        /// <summary>
        /// The main controller for Persistent Cloud Anchors sample.
        /// </summary>
        public PersistentCloudAnchorsController Controller;

        /// <summary>
        /// The 3D object that represents a Cloud Anchor.
        /// </summary>
        public GameObject CloudAnchorPrefab;

        /// <summary>
        /// The game object that includes <see cref="MapQualityIndicator"/> to visualize
        /// map quality result.
        /// </summary>
        public GameObject MapQualityIndicatorPrefab;

        /// <summary>
        /// The UI element that displays the instructions to guide hosting experience.
        /// </summary>
        public GameObject InstructionBar;

        /// <summary>
        /// The instruction text in the top instruction bar.
        /// </summary>
        public Text InstructionText;

        /// <summary>
        /// Display the tracking helper text when the session in not tracking.
        /// </summary>
        public Text TrackingHelperText;

        /// <summary>
        /// The debug text in bottom snack bar.
        /// </summary>
        public Text DebugText;

        /// <summary>
        /// The button to save current cloud anchor id into clipboard.
        /// </summary>
        public Button ShareButton;

        /// <summary>
        /// <summary>
        /// The button to clear all placed anchors.
        /// </summary>
        public Button ClearAnchorsButton;

        /// <summary>
        /// Text showing the count of placed anchors.
        /// </summary>
        public Text AnchorCountText;

        /// <summary>
        /// The UI panel for naming individual anchors.
        /// </summary>
        public GameObject IndividualAnchorNamePanel;

        /// <summary>
        /// The input field for naming individual anchors.
        /// </summary>
        public InputField IndividualAnchorNameField;

        /// <summary>
        /// The button to confirm the anchor name.
        /// </summary>
        public Button ConfirmAnchorNameButton;

        /// <summary>
        /// The button to cancel anchor naming (removes the anchor).
        /// </summary>
        public Button CancelAnchorNameButton;

        /// <summary>
        /// Helper message for <see cref="NotTrackingReason.Initializing">.</see>
        /// </summary>
        private const string _initializingMessage = "Tracking is being initialized.";

        /// <summary>
        /// Helper message for <see cref="NotTrackingReason.Relocalizing">.</see>
        /// </summary>
        private const string _relocalizingMessage = "Tracking is resuming after an interruption.";

        /// <summary>
        /// Helper message for <see cref="NotTrackingReason.InsufficientLight">.</see>
        /// </summary>
        private const string _insufficientLightMessage = "Too dark. Try moving to a well-lit area.";

        /// <summary>
        /// Helper message for <see cref="NotTrackingReason.InsufficientLight">
        /// in Android S or above.</see>
        /// </summary>
        private const string _insufficientLightMessageAndroidS =
            "Too dark. Try moving to a well-lit area. " +
            "Also, make sure the Block Camera is set to off in system settings.";

        /// <summary>
        /// Helper message for <see cref="NotTrackingReason.InsufficientFeatures">.</see>
        /// </summary>
        private const string _insufficientFeatureMessage =
            "Can't find anything. Aim device at a surface with more texture or color.";

        /// <summary>
        /// Helper message for <see cref="NotTrackingReason.ExcessiveMotion">.</see>
        /// </summary>
        private const string _excessiveMotionMessage = "Moving too fast. Slow down.";

        /// <summary>
        /// Helper message for <see cref="NotTrackingReason.Unsupported">.</see>
        /// </summary>
        private const string _unsupportedMessage = "Tracking lost reason is not supported.";

        /// <summary>
        /// The time between enters AR View and ARCore session starts to host or resolve.
        /// </summary>
        private const float _startPrepareTime = 3.0f;

        /// <summary>
        /// Android 12 (S) SDK version.
        /// </summary>
        private const int _androidSSDKVesion = 31;

        /// <summary>
        /// Pixel Model keyword.
        /// </summary>
        private const string _pixelModel = "pixel";

        /// <summary>
        /// The timer to indicate whether the AR View has passed the start prepare time.
        /// </summary>
        private float _timeSinceStart;

        /// <summary>
        /// True if the app is in the process of returning to home page due to an invalid state,
        /// otherwise false.
        /// </summary>
        private bool _isReturning;

        /// <summary>
        /// The MapQualityIndicator that attaches to the placed object.
        /// </summary>
        private MapQualityIndicator _qualityIndicator = null;

        /// <summary>
        /// The history data that represents the current hosted Cloud Anchors.
        /// </summary>
        private List<CloudAnchorHistory> _hostedCloudAnchors = new List<CloudAnchorHistory>();

        /// <summary>
        /// A list of ARAnchor objects indicating the 3D objects placed on flat surfaces
        /// and waiting for hosting.
        /// </summary>
        private List<ARAnchor> _anchors = new List<ARAnchor>();

        /// <summary>
        /// The promises for the async hosting operations, if any.
        /// </summary>
        private List<HostCloudAnchorPromise> _hostPromises = new List<HostCloudAnchorPromise>();

        /// <summary>
        /// The results of the hosting operations, if any.
        /// </summary>
        private List<HostCloudAnchorResult> _hostResults = new List<HostCloudAnchorResult>();

        /// <summary>
        /// The coroutines for the hosting operations, if any.
        /// </summary>
        private List<IEnumerator> _hostCoroutines = new List<IEnumerator>();

        /// <summary>
        /// Quality indicators for each anchor.
        /// </summary>
        private List<MapQualityIndicator> _qualityIndicators = new List<MapQualityIndicator>();

        /// <summary>
        /// Names assigned to each anchor by the user.
        /// </summary>
        private List<string> _anchorNames = new List<string>();

        /// <summary>
        /// Game objects instantiated for each anchor (contains the 3D model and text).
        /// </summary>
        private List<GameObject> _anchorGameObjects = new List<GameObject>();

        /// <summary>
        /// Index of the anchor currently being named (-1 if none).
        /// </summary>
        private int _currentNamingAnchorIndex = -1;

        /// <summary>
        /// The promises for the async resolving operations, if any.
        /// </summary>
        private List<ResolveCloudAnchorPromise> _resolvePromises =
            new List<ResolveCloudAnchorPromise>();

        /// <summary>
        /// The results of the resolving operations, if any.
        /// </summary>
        private List<ResolveCloudAnchorResult> _resolveResults =
            new List<ResolveCloudAnchorResult>();

        /// <summary>
        /// The coroutines of the resolving operations, if any.
        /// </summary>
        private List<IEnumerator> _resolveCoroutines = new List<IEnumerator>();

        private AndroidJavaClass _versionInfo;

        /// <summary>
        /// Get the camera pose for the current frame.
        /// </summary>
        /// <returns>The camera pose of the current frame.</returns>
        public Pose GetCameraPose()
        {
            return new Pose(Controller.MainCamera.transform.position,
                Controller.MainCamera.transform.rotation);
        }

        /// <summary>
        /// Callback handling "Share" button click event.
        /// </summary>
        public void OnShareButtonClicked()
        {
            if (_hostedCloudAnchors.Count > 0)
            {
                // Share all hosted anchor IDs
                string allIds = string.Join("\n", _hostedCloudAnchors.ConvertAll(anchor => anchor.Id));
                GUIUtility.systemCopyBuffer = allIds;
                DebugText.text = $"Copied {_hostedCloudAnchors.Count} cloud anchor IDs to clipboard";
            }
        }

        /// <summary>
        /// Callback handling "Finish Hosting" button click event.
        /// </summary>
        public void OnFinishHostingButtonClicked()
        {
            if (_anchors.Count == 0)
            {
                DebugText.text = "No anchors to host. Place some anchors first.";
                return;
            }

            StartHostingAllAnchors();
        }

        /// <summary>
        /// Callback handling "Clear Anchors" button click event.
        /// </summary>
        public void OnClearAnchorsButtonClicked()
        {
            ClearAllAnchors();
            UpdateAnchorCountDisplay();
            DebugText.text = "All anchors cleared.";
        }

        /// <summary>
        /// Callback handling "Confirm Anchor Name" button click event.
        /// </summary>
        public void OnConfirmAnchorNameButtonClicked()
        {
            if (_currentNamingAnchorIndex >= 0 && _currentNamingAnchorIndex < _anchorNames.Count)
            {
                string anchorName = IndividualAnchorNameField.text.Trim();
                if (string.IsNullOrEmpty(anchorName))
                {
                    anchorName = $"Anchor_{_currentNamingAnchorIndex + 1}";
                }

                // Store the name
                _anchorNames[_currentNamingAnchorIndex] = anchorName;

                // Update the 3D text on the anchor
                UpdateAnchor3DText(_currentNamingAnchorIndex, anchorName);

                // Hide the naming panel
                IndividualAnchorNamePanel.SetActive(false);
                _currentNamingAnchorIndex = -1;

                DebugText.text = $"Anchor named: {anchorName}";
            }
        }

        /// <summary>
        /// Callback handling "Cancel Anchor Name" button click event.
        /// </summary>
        public void OnCancelAnchorNameButtonClicked()
        {
            if (_currentNamingAnchorIndex >= 0 && _currentNamingAnchorIndex < _anchors.Count)
            {
                // Remove the anchor that was just placed
                RemoveAnchor(_currentNamingAnchorIndex);
                
                // Hide the naming panel
                IndividualAnchorNamePanel.SetActive(false);
                _currentNamingAnchorIndex = -1;

                DebugText.text = "Anchor placement cancelled.";
                UpdateAnchorCountDisplay();
            }
        }

        /// <summary>
        /// The Unity Awake() method.
        /// </summary>
        public void Awake()
        {
            _versionInfo = new AndroidJavaClass("android.os.Build$VERSION");
        }

        /// <summary>
        /// The Unity OnEnable() method.
        /// </summary>
        public void OnEnable()
        {
            _timeSinceStart = 0.0f;
            _isReturning = false;
            ClearAllAnchors();
            
            InstructionBar.SetActive(true);
            ShareButton.gameObject.SetActive(false);
            
            // Setup UI for multiple anchors
            if (ClearAnchorsButton != null)
                ClearAnchorsButton.gameObject.SetActive(Controller.Mode == PersistentCloudAnchorsController.ApplicationMode.Hosting);
            
            // Setup individual anchor naming UI
            if (IndividualAnchorNamePanel != null)
                IndividualAnchorNamePanel.SetActive(false);
            
            UpdatePlaneVisibility(true);
            UpdateAnchorCountDisplay();

            switch (Controller.Mode)
            {
                case PersistentCloudAnchorsController.ApplicationMode.Ready:
                    ReturnToHomePage("Invalid application mode, returning to home page...");
                    break;
                case PersistentCloudAnchorsController.ApplicationMode.Hosting:
                case PersistentCloudAnchorsController.ApplicationMode.Resolving:
                    InstructionText.text = "Detecting flat surface...";
                    DebugText.text = "ARCore is preparing for " + Controller.Mode;
                    break;
            }
        }

        /// <summary>
        /// The Unity OnDisable() method.
        /// </summary>
        public void OnDisable()
        {
            ClearAllAnchors();
            UpdatePlaneVisibility(false);
        }

        /// <summary>
        /// Clear all anchors and associated data.
        /// </summary>
        private void ClearAllAnchors()
        {
            // Clear quality indicators
            foreach (var indicator in _qualityIndicators)
            {
                if (indicator != null)
                {
                    Destroy(indicator.gameObject);
                }
            }
            _qualityIndicators.Clear();

            // Clear anchor game objects
            foreach (var anchorObject in _anchorGameObjects)
            {
                if (anchorObject != null)
                {
                    Destroy(anchorObject);
                }
            }
            _anchorGameObjects.Clear();

            // Clear anchors
            foreach (var anchor in _anchors)
            {
                if (anchor != null)
                {
                    Destroy(anchor.gameObject);
                }
            }
            _anchors.Clear();

            // Clear anchor names
            _anchorNames.Clear();

            // Reset naming state
            _currentNamingAnchorIndex = -1;
            if (IndividualAnchorNamePanel != null)
            {
                IndividualAnchorNamePanel.SetActive(false);
            };

            // Clear hosting coroutines
            foreach (var coroutine in _hostCoroutines)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            _hostCoroutines.Clear();

            // Clear hosting promises
            foreach (var promise in _hostPromises)
            {
                if (promise != null)
                {
                    promise.Cancel();
                }
            }
            _hostPromises.Clear();

            // Clear hosting results
            _hostResults.Clear();
            _hostedCloudAnchors.Clear();

            // Clear resolving data
            foreach (var coroutine in _resolveCoroutines)
            {
                StopCoroutine(coroutine);
            }
            _resolveCoroutines.Clear();

            foreach (var promise in _resolvePromises)
            {
                promise.Cancel();
            }
            _resolvePromises.Clear();

            foreach (var result in _resolveResults)
            {
                if (result.Anchor != null)
                {
                    Destroy(result.Anchor.gameObject);
                }
            }
            _resolveResults.Clear();
        }

        /// <summary>
        /// Update the anchor count display.
        /// </summary>
        private void UpdateAnchorCountDisplay()
        {
            if (AnchorCountText != null)
            {
                AnchorCountText.text = $"Anchors: {_anchors.Count}";
            }
        }

        /// <summary>
        /// Update the 3D text on a specific anchor.
        /// </summary>
        /// <param name="anchorIndex">Index of the anchor to update</param>
        /// <param name="text">Text to display</param>
        private void UpdateAnchor3DText(int anchorIndex, string text)
        {
            if (anchorIndex >= 0 && anchorIndex < _anchorGameObjects.Count)
            {
                GameObject anchorObject = _anchorGameObjects[anchorIndex];
                if (anchorObject != null)
                {
                    UpdateAnchorObjectText(anchorObject, text);
                }
            }
        }

        /// <summary>
        /// Update the 3D text on a resolved anchor object.
        /// </summary>
        /// <param name="anchorObject">The anchor GameObject</param>
        /// <param name="text">Text to display</param>
        private void UpdateResolvedAnchor3DText(GameObject anchorObject, string text)
        {
            UpdateAnchorObjectText(anchorObject, text);
        }

        /// <summary>
        /// Update the 3D text on any anchor object.
        /// </summary>
        /// <param name="anchorObject">The anchor GameObject</param>
        /// <param name="text">Text to display</param>
        private void UpdateAnchorObjectText(GameObject anchorObject, string text)
        {
            if (anchorObject != null)
            {
                // Look for TextMesh component in the anchor object and its children
                TextMesh textMesh = anchorObject.GetComponentInChildren<TextMesh>();
                if (textMesh != null)
                {
                    textMesh.text = text;
                }
                else
                {
                    // Also check for TextMeshPro components if using TextMeshPro
                    var textMeshPro = anchorObject.GetComponentInChildren<TMPro.TextMeshPro>();
                    if (textMeshPro != null)
                    {
                        textMeshPro.text = text;
                    }
                    else
                    {
                        Debug.LogWarning($"No TextMesh or TextMeshPro component found on anchor object. Please ensure your CloudAnchorPrefab has a TextMesh or TextMeshPro component.");
                    }
                }
            }
        }

        /// <summary>
        /// Remove a specific anchor from all collections.
        /// </summary>
        /// <param name="anchorIndex">Index of the anchor to remove</param>
        private void RemoveAnchor(int anchorIndex)
        {
            if (anchorIndex >= 0 && anchorIndex < _anchors.Count)
            {
                // Destroy the anchor GameObject
                if (anchorIndex < _anchorGameObjects.Count && _anchorGameObjects[anchorIndex] != null)
                {
                    Destroy(_anchorGameObjects[anchorIndex]);
                    _anchorGameObjects.RemoveAt(anchorIndex);
                }

                // Destroy the quality indicator
                if (anchorIndex < _qualityIndicators.Count && _qualityIndicators[anchorIndex] != null)
                {
                    Destroy(_qualityIndicators[anchorIndex].gameObject);
                    _qualityIndicators.RemoveAt(anchorIndex);
                }

                // Destroy the anchor itself
                if (_anchors[anchorIndex] != null)
                {
                    Destroy(_anchors[anchorIndex].gameObject);
                }
                _anchors.RemoveAt(anchorIndex);

                // Remove from names list
                if (anchorIndex < _anchorNames.Count)
                {
                    _anchorNames.RemoveAt(anchorIndex);
                }
            }
        }

        /// <summary>
        /// Start hosting all placed anchors.
        /// </summary>
        private void StartHostingAllAnchors()
        {
            InstructionText.text = "Hosting all anchors...";
            DebugText.text = $"Starting to host {_anchors.Count} anchors...";

            for (int i = 0; i < _anchors.Count; i++)
            {
                var anchor = _anchors[i];
                if (anchor != null)
                {
                    StartHostingSingleAnchor(anchor, i);
                }
            }
        }

        /// <summary>
        /// Start hosting a single anchor.
        /// </summary>
        private void StartHostingSingleAnchor(ARAnchor anchor, int index)
        {
            var promise = Controller.AnchorManager.HostCloudAnchorAsync(anchor, 1);
            if (promise.State == PromiseState.Done)
            {
                Debug.LogFormat("Failed to host anchor {0}.", index);
                OnSingleAnchorHostedFinished(false, index);
            }
            else
            {
                _hostPromises.Add(promise);
                var coroutine = HostSingleAnchor(promise, index);
                _hostCoroutines.Add(coroutine);
                StartCoroutine(coroutine);
            }
        }

        /// <summary>
        /// Coroutine for hosting a single anchor.
        /// </summary>
        private IEnumerator HostSingleAnchor(HostCloudAnchorPromise promise, int index)
        {
            yield return promise;
            var result = promise.Result;

            if (result.CloudAnchorState == CloudAnchorState.Success)
            {
                // Use the custom anchor name if available, otherwise use default naming
                string anchorName = (index < _anchorNames.Count && !string.IsNullOrEmpty(_anchorNames[index])) 
                    ? _anchorNames[index] 
                    : $"Anchor_{index + 1}";
                
                var hostedAnchor = new CloudAnchorHistory(anchorName, result.CloudAnchorId);
                _hostedCloudAnchors.Add(hostedAnchor);
                Controller.SaveCloudAnchorHistory(hostedAnchor);
                OnSingleAnchorHostedFinished(true, index, result.CloudAnchorId);
            }
            else
            {
                OnSingleAnchorHostedFinished(false, index, result.CloudAnchorState.ToString());
            }
        }

        /// <summary>
        /// Handle completion of hosting a single anchor.
        /// </summary>
        private void OnSingleAnchorHostedFinished(bool success, int index, string response = null)
        {
            if (success)
            {
                DebugText.text = $"Successfully hosted anchor {index + 1}: {response}";
            }
            else
            {
                DebugText.text = $"Failed to host anchor {index + 1}: {response}";
            }

            // Check if all anchors are processed
            if (_hostedCloudAnchors.Count + GetFailedHostingCount() >= _anchors.Count)
            {
                OnAllAnchorsHostedFinished();
            }
        }

        /// <summary>
        /// Get the count of failed hosting attempts.
        /// </summary>
        private int GetFailedHostingCount()
        {
            int failedCount = 0;
            foreach (var promise in _hostPromises)
            {
                if (promise.State == PromiseState.Done && promise.Result.CloudAnchorState != CloudAnchorState.Success)
                {
                    failedCount++;
                }
            }
            return failedCount;
        }

        /// <summary>
        /// Handle completion of hosting all anchors.
        /// </summary>
        private void OnAllAnchorsHostedFinished()
        {
            InstructionText.text = "Hosting complete!";
            DebugText.text = $"Hosted {_hostedCloudAnchors.Count} out of {_anchors.Count} anchors successfully.";
            
            if (_hostedCloudAnchors.Count > 0)
            {
                ShareButton.gameObject.SetActive(true);
                // Automatically save with timestamp-based names instead of showing name panel
                for (int i = 0; i < _hostedCloudAnchors.Count; i++)
                {
                    var anchor = _hostedCloudAnchors[i];
                    // Use individual name if available, otherwise use timestamp-based naming
                    if (string.IsNullOrEmpty(anchor.Name))
                    {
                        anchor.Name = $"Anchor_{System.DateTime.Now:yyyyMMdd_HHmmss}_{i + 1}";
                    }
                    _hostedCloudAnchors[i] = anchor;
                    Controller.SaveCloudAnchorHistory(anchor);
                }
                DebugText.text = $"All {_hostedCloudAnchors.Count} anchors saved successfully!";
            }
        }

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            // Give ARCore some time to prepare for hosting or resolving.
            if (_timeSinceStart < _startPrepareTime)
            {
                _timeSinceStart += Time.deltaTime;
                if (_timeSinceStart >= _startPrepareTime)
                {
                    UpdateInitialInstruction();
                }

                return;
            }

            ARCoreLifecycleUpdate();
            if (_isReturning)
            {
                return;
            }

            if (_timeSinceStart >= _startPrepareTime)
            {
                DisplayTrackingHelperMessage();
            }

            if (Controller.Mode == PersistentCloudAnchorsController.ApplicationMode.Resolving)
            {
                ResolvingCloudAnchors();
            }
            else if (Controller.Mode == PersistentCloudAnchorsController.ApplicationMode.Hosting)
            {
                // Allow placing multiple anchors by touch
                Touch touch;
                if (Input.touchCount >= 1 && (touch = Input.GetTouch(0)).phase == TouchPhase.Began)
                {
                    // Check if touch is over UI using multiple methods
                    if (!IsTouchOverUI(touch))
                    {
                        // Perform hit test and place a new anchor.
                        PerformHitTest(touch.position);
                    }
                }
            }
        }

        private void PerformHitTest(Vector2 touchPos)
        {
            List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
            Controller.RaycastManager.Raycast(
                touchPos, hitResults, TrackableType.PlaneWithinPolygon);

            // If there was an anchor placed, then instantiate the corresponding object.
            var planeType = PlaneAlignment.HorizontalUp;
            if (hitResults.Count > 0)
            {
                ARPlane plane = Controller.PlaneManager.GetPlane(hitResults[0].trackableId);
                if (plane == null)
                {
                    Debug.LogWarningFormat("Failed to find the ARPlane with TrackableId {0}",
                        hitResults[0].trackableId);
                    return;
                }

                planeType = plane.alignment;
                var hitPose = hitResults[0].pose;
                if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    // Point the hitPose rotation roughly away from the raycast/camera
                    // to match ARCore.
                    hitPose.rotation.eulerAngles =
                        new Vector3(0.0f, Controller.MainCamera.transform.eulerAngles.y, 0.0f);
                }

                var newAnchor = Controller.AnchorManager.AttachAnchor(plane, hitPose);
                if (newAnchor != null)
                {
                    _anchors.Add(newAnchor);
                    
                    // Instantiate the visual object and store reference
                    GameObject anchorObject = Instantiate(CloudAnchorPrefab, newAnchor.transform);
                    _anchorGameObjects.Add(anchorObject);

                    // Attach map quality indicator to this anchor
                    var indicatorGO = Instantiate(MapQualityIndicatorPrefab, newAnchor.transform);
                    var qualityIndicator = indicatorGO.GetComponent<MapQualityIndicator>();
                    qualityIndicator.DrawIndicator(planeType, Controller.MainCamera);
                    _qualityIndicators.Add(qualityIndicator);

                    // Add placeholder name
                    _anchorNames.Add("");

                    // Show naming UI for this anchor
                    _currentNamingAnchorIndex = _anchors.Count - 1;
                    if (IndividualAnchorNamePanel != null)
                    {
                        IndividualAnchorNamePanel.SetActive(true);
                        if (IndividualAnchorNameField != null)
                        {
                            IndividualAnchorNameField.text = $"Anchor_{_anchors.Count}";
                            IndividualAnchorNameField.Select();
                        }
                    }

                    // Update UI
                    UpdateAnchorCountDisplay();
                    InstructionText.text = $"Name your anchor (Anchor {_anchors.Count})";
                    DebugText.text = $"Placed anchor {_anchors.Count}. Please give it a name.";
                }
            }
        }

        /// <summary>
        /// Check if touch is over UI elements using multiple detection methods.
        /// </summary>
        /// <param name="touch">The touch input to check</param>
        /// <returns>True if touch is over UI, false otherwise</returns>
        private bool IsTouchOverUI(Touch touch)
        {
            // Method 1: Standard EventSystem check with fingerId
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                return true;
            }

            // Method 2: EventSystem check without fingerId (fallback for some devices)
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }

            // Method 3: Manual raycast to check for UI elements
            var eventData = new PointerEventData(EventSystem.current);
            eventData.position = touch.position;
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            // Check if any UI element was hit
            foreach (var result in results)
            {
                // Check for common UI components that should block touch
                if (result.gameObject.GetComponent<Button>() != null ||
                    result.gameObject.GetComponent<Toggle>() != null ||
                    result.gameObject.GetComponent<Slider>() != null ||
                    result.gameObject.GetComponent<InputField>() != null ||
                    result.gameObject.GetComponent<Dropdown>() != null ||
                    result.gameObject.GetComponent<ScrollRect>() != null)
                {
                    return true;
                }

                // Check if the object has the "UI" tag or is in a Canvas
                if (result.gameObject.tag == "UI" || 
                    result.gameObject.GetComponentInParent<Canvas>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        /* LEGACY METHOD - NOW USING MULTI-ANCHOR APPROACH
        private void HostingCloudAnchor()
        {
            // There is no anchor for hosting.
            if (_anchor == null)
            {
                return;
            }

            // There is a pending or finished hosting task.
            if (_hostPromise != null || _hostResult != null)
            {
                return;
            }

            // Update map quality:
            int qualityState = 2;
            // Can pass in ANY valid camera pose to the mapping quality API.
            // Ideally, the pose should represent users’ expected perspectives.
            FeatureMapQuality quality =
                Controller.AnchorManager.EstimateFeatureMapQualityForHosting(GetCameraPose());
            DebugText.text = "Current mapping quality: " + quality;
            qualityState = (int)quality;
            _qualityIndicator.UpdateQualityState(qualityState);

            // Hosting instructions:
            var cameraDist = (_qualityIndicator.transform.position -
                Controller.MainCamera.transform.position).magnitude;
            if (cameraDist < _qualityIndicator.Radius * 1.5f)
            {
                InstructionText.text = "You are too close, move backward.";
                return;
            }
            else if (cameraDist > 10.0f)
            {
                InstructionText.text = "You are too far, come closer.";
                return;
            }
            else if (_qualityIndicator.ReachTopviewAngle)
            {
                InstructionText.text =
                    "You are looking from the top view, move around from all sides.";
                return;
            }
            else if (!_qualityIndicator.ReachQualityThreshold)
            {
                InstructionText.text = "Save the object here by capturing it from all sides.";
                return;
            }

            // Start hosting:
            InstructionText.text = "Processing...";
            DebugText.text = "Mapping quality has reached sufficient threshold, " +
                "creating Cloud Anchor.";
            DebugText.text = string.Format(
                "FeatureMapQuality has reached {0}, triggering CreateCloudAnchor.",
                Controller.AnchorManager.EstimateFeatureMapQualityForHosting(GetCameraPose()));

            // Creating a Cloud Anchor with lifetime = 1 day.
            // This is configurable up to 365 days when keyless authentication is used.
            var promise = Controller.AnchorManager.HostCloudAnchorAsync(_anchor, 1);
            if (promise.State == PromiseState.Done)
            {
                Debug.LogFormat("Failed to host a Cloud Anchor.");
                OnAnchorHostedFinished(false);
            }
            else
            {
                _hostPromise = promise;
                _hostCoroutine = HostAnchor();
                StartCoroutine(_hostCoroutine);
            }
        }

        private IEnumerator HostAnchor()
        {
            yield return _hostPromise;
            _hostResult = _hostPromise.Result;
            _hostPromise = null;

            if (_hostResult.CloudAnchorState == CloudAnchorState.Success)
            {
                int count = Controller.LoadCloudAnchorHistory().Collection.Count;
                _hostedCloudAnchor =
                    new CloudAnchorHistory("CloudAnchor" + count, _hostResult.CloudAnchorId);
                OnAnchorHostedFinished(true, _hostResult.CloudAnchorId);
            }
            else
            {
                OnAnchorHostedFinished(false, _hostResult.CloudAnchorState.ToString());
            }
        }
        */ // END LEGACY METHODS

        private void ResolvingCloudAnchors()
        {
            // No Cloud Anchor for resolving.
            if (Controller.ResolvingSet.Count == 0)
            {
                return;
            }

            // There are pending or finished resolving tasks.
            if (_resolvePromises.Count > 0 || _resolveResults.Count > 0)
            {
                return;
            }

            // ARCore session is not ready for resolving.
            if (ARSession.state != ARSessionState.SessionTracking)
            {
                return;
            }

            Debug.LogFormat("Attempting to resolve {0} Cloud Anchor(s): {1}",
                Controller.ResolvingSet.Count,
                string.Join(",", new List<string>(Controller.ResolvingSet).ToArray()));
            foreach (string cloudId in Controller.ResolvingSet)
            {
                var promise = Controller.AnchorManager.ResolveCloudAnchorAsync(cloudId);
                if (promise.State == PromiseState.Done)
                {
                    Debug.LogFormat("Faild to resolve Cloud Anchor " + cloudId);
                    OnAnchorResolvedFinished(false, cloudId);
                }
                else
                {
                    _resolvePromises.Add(promise);
                    var coroutine = ResolveAnchor(cloudId, promise);
                    StartCoroutine(coroutine);
                }
            }

            Controller.ResolvingSet.Clear();
        }

        private IEnumerator ResolveAnchor(string cloudId, ResolveCloudAnchorPromise promise)
        {
            yield return promise;
            var result = promise.Result;
            _resolvePromises.Remove(promise);
            _resolveResults.Add(result);

            if (result.CloudAnchorState == CloudAnchorState.Success)
            {
                OnAnchorResolvedFinished(true, cloudId);
                GameObject resolvedAnchorObject = Instantiate(CloudAnchorPrefab, result.Anchor.transform);
                
                // Find the anchor name from history and display it
                var history = Controller.LoadCloudAnchorHistory();
                var anchorData = history.Collection.Find(anchor => anchor.Id == cloudId);
                if (!string.IsNullOrEmpty(anchorData.Id) && !string.IsNullOrEmpty(anchorData.Name))
                {
                    // Update the 3D text on the resolved anchor
                    UpdateResolvedAnchor3DText(resolvedAnchorObject, anchorData.Name);
                }
            }
            else
            {
                OnAnchorResolvedFinished(false, cloudId, result.CloudAnchorState.ToString());
            }
        }

        /* LEGACY SINGLE ANCHOR METHOD - REPLACED WITH MULTI-ANCHOR SUPPORT
        private void OnAnchorHostedFinished(bool success, string response = null)
        {
            if (success)
            {
                InstructionText.text = "Finish!";
                Invoke("DoHideInstructionBar", 1.5f);
                DebugText.text =
                    string.Format("Succeed to host the Cloud Anchor: {0}.", response);

                // Display name panel and hide instruction bar.
                NameField.text = _hostedCloudAnchor.Name;
                NamePanel.SetActive(true);
                SetSaveButtonActive(true);
            }
            else
            {
                InstructionText.text = "Host failed.";
                DebugText.text = "Failed to host a Cloud Anchor" + (response == null ? "." :
                    "with error " + response + ".");
            }
        }
        */

        private void OnAnchorResolvedFinished(bool success, string cloudId, string response = null)
        {
            if (success)
            {
                InstructionText.text = "Resolve success!";
                DebugText.text =
                    string.Format("Succeed to resolve the Cloud Anchor: {0}.", cloudId);
            }
            else
            {
                InstructionText.text = "Resolve failed.";
                DebugText.text = "Failed to resolve Cloud Anchor: " + cloudId +
                    (response == null ? "." : "with error " + response + ".");
            }
        }

        private void UpdateInitialInstruction()
        {
            switch (Controller.Mode)
            {
                case PersistentCloudAnchorsController.ApplicationMode.Hosting:
                    // Initial instruction for hosting flow:
                    InstructionText.text = "Tap to place multiple objects. Use 'Finish Hosting' when done.";
                    DebugText.text = "Tap vertical or horizontal planes to place anchors...";
                    return;
                case PersistentCloudAnchorsController.ApplicationMode.Resolving:
                    // Initial instruction for resolving flow:
                    InstructionText.text =
                        "Look at the location you expect to see the AR experience appear.";
                    DebugText.text = string.Format("Attempting to resolve {0} anchors...",
                        Controller.ResolvingSet.Count);
                    return;
                default:
                    return;
            }
        }

        private void UpdatePlaneVisibility(bool visible)
        {
            foreach (var plane in Controller.PlaneManager.trackables)
            {
                plane.gameObject.SetActive(visible);
            }
        }

        private void ARCoreLifecycleUpdate()
        {
            // Only allow the screen to sleep when not tracking.
            var sleepTimeout = SleepTimeout.NeverSleep;
            if (ARSession.state != ARSessionState.SessionTracking)
            {
                sleepTimeout = SleepTimeout.SystemSetting;
            }

            Screen.sleepTimeout = sleepTimeout;

            if (_isReturning)
            {
                return;
            }

            // Return to home page if ARSession is in error status.
            if (ARSession.state != ARSessionState.Ready &&
                ARSession.state != ARSessionState.SessionInitializing &&
                ARSession.state != ARSessionState.SessionTracking)
            {
                ReturnToHomePage(string.Format(
                    "ARCore encountered an error state {0}. Please start the app again.",
                    ARSession.state));
            }
        }

        private void DisplayTrackingHelperMessage()
        {
            if (_isReturning || ARSession.notTrackingReason == NotTrackingReason.None)
            {
                TrackingHelperText.gameObject.SetActive(false);
            }
            else
            {
                TrackingHelperText.gameObject.SetActive(true);
                switch (ARSession.notTrackingReason)
                {
                    case NotTrackingReason.Initializing:
                        TrackingHelperText.text = _initializingMessage;
                        return;
                    case NotTrackingReason.Relocalizing:
                        TrackingHelperText.text = _relocalizingMessage;
                        return;
                    case NotTrackingReason.InsufficientLight:
                        if (_versionInfo.GetStatic<int>("SDK_INT") < _androidSSDKVesion)
                        {
                            TrackingHelperText.text = _insufficientLightMessage;
                        }
                        else
                        {
                            TrackingHelperText.text = _insufficientLightMessageAndroidS;
                        }

                        return;
                    case NotTrackingReason.InsufficientFeatures:
                        TrackingHelperText.text = _insufficientFeatureMessage;
                        return;
                    case NotTrackingReason.ExcessiveMotion:
                        TrackingHelperText.text = _excessiveMotionMessage;
                        return;
                    case NotTrackingReason.Unsupported:
                        TrackingHelperText.text = _unsupportedMessage;
                        return;
                    default:
                        TrackingHelperText.text =
                            string.Format("Not tracking reason: {0}", ARSession.notTrackingReason);
                        return;
                }
            }
        }

        private void ReturnToHomePage(string reason)
        {
            Debug.Log("Returning home for reason: " + reason);
            if (_isReturning)
            {
                return;
            }

            _isReturning = true;
            DebugText.text = reason;
            Invoke("DoReturnToHomePage", 3.0f);
        }

        private void DoReturnToHomePage()
        {
            Controller.SwitchToHomePage();
        }

        private void DoHideInstructionBar()
        {
            InstructionBar.SetActive(false);
        }
    }
}
