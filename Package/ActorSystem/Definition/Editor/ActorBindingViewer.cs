using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

namespace KahaGameCore.Package.ActorSystem.Definition.Editor
{
    public class ActorBindingViewer : EditorWindow
    {
        private Vector2 scrollPosition;
        private string searchFilter = "";
        private bool autoRefresh = true;
        private double lastRefreshTime;
        private const double refreshInterval = 1.0; // Refresh every 1 second when auto-refresh is enabled
        private Dictionary<string, bool> actorFoldouts = new Dictionary<string, bool>();
        private bool showInactiveControllers = true;

        // Reflection fields to access private members of ActorCollection
        private FieldInfo actorsFieldInfo;

        [MenuItem("Tools/Actor Binding Viewer")]
        public static void ShowWindow()
        {
            GetWindow<ActorBindingViewer>("Actor Binding Viewer");
        }

        private void OnEnable()
        {
            // Use reflection to access the private actors list in ActorCollection
            actorsFieldInfo = typeof(ActorCollection).GetField("actors", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private void OnGUI()
        {
            // Initialize styles in OnGUI since GUIStyle can only be used here
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;

            GUIStyle subHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            subHeaderStyle.fontSize = 12;

            GUIStyle activeControllerStyle = new GUIStyle(EditorStyles.label);
            activeControllerStyle.normal.textColor = new Color(0.0f, 0.6f, 0.0f); // Green for active

            GUIStyle lockedControllerStyle = new GUIStyle(EditorStyles.label);
            lockedControllerStyle.normal.textColor = new Color(0.7f, 0.0f, 0.0f); // Red for locked

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Title
            EditorGUILayout.LabelField("Actor Binding Viewer", headerStyle);
            EditorGUILayout.Space();

            // Search and refresh controls
            EditorGUILayout.BeginHorizontal();

            // Search field
            string newSearchFilter = EditorGUILayout.TextField("Search", searchFilter);
            if (newSearchFilter != searchFilter)
            {
                searchFilter = newSearchFilter;
                Repaint();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            // Auto-refresh toggle
            bool newAutoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);
            if (newAutoRefresh != autoRefresh)
            {
                autoRefresh = newAutoRefresh;
                Repaint();
            }

            // Show inactive controllers toggle
            bool newShowInactive = EditorGUILayout.Toggle("Show Inactive Controllers", showInactiveControllers);
            if (newShowInactive != showInactiveControllers)
            {
                showInactiveControllers = newShowInactive;
                Repaint();
            }

            // Manual refresh button
            if (GUILayout.Button("Refresh", GUILayout.Width(100)))
            {
                Repaint();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Get actors from ActorCollection
            List<Actor> actors = GetActors();

            if (actors == null || actors.Count == 0)
            {
                EditorGUILayout.LabelField("No actors found in the scene.");
                EditorGUILayout.EndVertical();
                return;
            }

            // Display actor count
            EditorGUILayout.LabelField($"Total Actors: {actors.Count}", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Begin scrollable area
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Display each actor and its controllers
            foreach (Actor actor in actors)
            {
                if (actor.Instance == null)
                    continue;

                string instanceName = actor.Instance.gameObject.name;

                // Apply search filter
                if (!string.IsNullOrEmpty(searchFilter) &&
                    !instanceName.ToLower().Contains(searchFilter.ToLower()))
                    continue;

                // Ensure this actor has an entry in the foldout dictionary
                string actorKey = actor.Instance.GetInstanceID().ToString();
                if (!actorFoldouts.ContainsKey(actorKey))
                {
                    actorFoldouts[actorKey] = true; // Default to expanded
                }

                // Actor foldout header with background
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Actor instance information with foldout
                EditorGUILayout.BeginHorizontal();

                // Foldout control
                actorFoldouts[actorKey] = EditorGUILayout.Foldout(actorFoldouts[actorKey], "", true);

                // Actor name and basic info
                EditorGUILayout.LabelField($"Actor: {instanceName}", subHeaderStyle);

                GUILayout.FlexibleSpace();

                // Active state indicator
                bool isActive = actor.Instance.gameObject.activeInHierarchy;
                EditorGUILayout.LabelField(isActive ? "Active" : "Inactive",
                    isActive ? activeControllerStyle : lockedControllerStyle,
                    GUILayout.Width(60));

                // Button to select the actor instance in the hierarchy
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = actor.Instance.gameObject;
                }

                EditorGUILayout.EndHorizontal();

                // Display position information
                Vector3 position = actor.Instance.transform.position;
                EditorGUILayout.LabelField($"Position: ({position.x:F2}, {position.y:F2}, {position.z:F2})");

                // Only show controllers if the actor is expanded
                if (actorFoldouts[actorKey])
                {
                    // Get controllers using reflection
                    List<ControllerBase> controllers = GetControllers(actor);

                    if (controllers != null && controllers.Count > 0)
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("Bound Controllers:", EditorStyles.boldLabel);
                        EditorGUI.indentLevel++;

                        // Display each controller
                        foreach (ControllerBase controller in controllers)
                        {
                            if (controller == null)
                                continue;

                            // Check if controller is locked
                            bool isLocked = controller.IsLocked();
                            bool isActorActive = controller.gameObject.activeInHierarchy && controller.enabled;

                            // Skip inactive controllers if option is disabled
                            if (!isActorActive && !showInactiveControllers)
                                continue;

                            EditorGUILayout.BeginHorizontal();

                            // Controller name and type with appropriate style
                            string controllerName = controller.gameObject.name;
                            string controllerType = controller.GetType().Name;
                            string statusText = isLocked ? "[Locked]" : (isActorActive ? "[Active]" : "[Inactive]");

                            // Choose style based on controller state
                            GUIStyle style = isLocked ? lockedControllerStyle :
                                             (isActorActive ? activeControllerStyle : EditorStyles.label);

                            // Display controller with status
                            EditorGUILayout.LabelField($"• {controllerName} ({controllerType}) {statusText}", style);

                            GUILayout.FlexibleSpace();

                            // Button to select the controller in the hierarchy
                            if (GUILayout.Button("Select", GUILayout.Width(60)))
                            {
                                Selection.activeGameObject = controller.gameObject;
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUI.indentLevel--;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No controllers bound to this actor.");
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void Update()
        {
            // Auto-refresh if enabled
            if (autoRefresh && EditorApplication.timeSinceStartup > lastRefreshTime + refreshInterval)
            {
                lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private List<Actor> GetActors()
        {
            // Get the ActorCollection instance
            ActorCollection actorCollection = ActorCollection.Instance;

            if (actorCollection == null || actorsFieldInfo == null)
                return new List<Actor>();

            // Use reflection to access the private actors list
            return actorsFieldInfo.GetValue(actorCollection) as List<Actor>;
        }

        private List<ControllerBase> GetControllers(Actor actor)
        {
            if (actor == null)
                return new List<ControllerBase>();

            // Use reflection to access the private controllers list in Actor
            FieldInfo controllersFieldInfo = typeof(Actor).GetField("controllers", BindingFlags.NonPublic | BindingFlags.Instance);

            if (controllersFieldInfo == null)
                return new List<ControllerBase>();

            return controllersFieldInfo.GetValue(actor) as List<ControllerBase>;
        }
    }
}
