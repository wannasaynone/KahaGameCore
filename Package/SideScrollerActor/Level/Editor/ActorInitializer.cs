using System.Collections.Generic;
using KahaGameCore.Package.SideScrollerActor.WeaponScripts;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    public class ActorInitializer : EditorWindow
    {
        [MenuItem("Tools/Actor Initializer")]
        public static void ShowWindow()
        {
            GetWindow<ActorInitializer>("Actor Initializer");
        }

        private void OnGUI()
        {
            List<Actor> actors = ActorContainer.GetActorsByCamp(Actor.Camp.Monster);
            List<Actor> heros = ActorContainer.GetActorsByCamp(Actor.Camp.Hero);

            GUILayout.Label("Actors in the scene", EditorStyles.boldLabel);

            HorizontalLine();

            GUILayout.Label($"Enemies: {actors.Count}");

            foreach (var actor in actors)
            {
                GUILayout.Label($"{actor.name}:{actor.GetStateName()}");
            }

            HorizontalLine();

            GUILayout.Label($"Heros: {heros.Count}");

            foreach (var actor in heros)
            {
                GUILayout.Label($"{actor.name}:{actor.GetStateName()}");
            }

            HorizontalLine();

            if (GUILayout.Button("Initialize Actors"))
            {
                InitializeActors();
            }

            if (Selection.activeGameObject != null)
            {
                HorizontalLine();
                Actor selectedActor = Selection.activeGameObject.GetComponent<Actor>();
                if (selectedActor != null)
                {
                    GUILayout.Label($"Selected Actor: {selectedActor.name}", EditorStyles.boldLabel);
                    if (selectedActor.IsControlable)
                    {
                        if (GUILayout.Button("Prepare Attack"))
                        {
                            selectedActor.StartPrepareAttack();
                        }

                        if (GUILayout.Button("End Prepare Attack"))
                        {
                            selectedActor.EndPrepareAttack();
                        }

                        if (GUILayout.Button("Attack"))
                        {
                            selectedActor.AttackWithWeapon();
                        }
                    }
                    else
                    {
                        GUILayout.Label("This actor is not controllable.", EditorStyles.boldLabel);
                    }
                }
            }
        }

        private void InitializeActors()
        {
            List<Actor> enemies = ActorContainer.GetActorsByCamp(Actor.Camp.Monster);

            for (int i = 0; i < enemies.Count; i++)
            {
                Weapon defaultWeapon = enemies[i].GetComponentInChildren<Weapon>();
                enemies[i].Initialize(defaultWeapon);
            }

            List<Actor> heros = ActorContainer.GetActorsByCamp(Actor.Camp.Hero);

            for (int i = 0; i < heros.Count; i++)
            {
                WeaponSwitcher weaponSwitcher = heros[i].GetComponentInChildren<WeaponSwitcher>();
                if (weaponSwitcher != null)
                {
                    weaponSwitcher.Initialize();
                }
                heros[i].Initialize(weaponSwitcher.GetDefaultWeapon());
            }
        }

        private static void HorizontalLine()
        {
            GUIStyle horizontalLine;
            horizontalLine = new GUIStyle();
            horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
            horizontalLine.margin = new RectOffset(0, 0, 4, 4);
            horizontalLine.fixedHeight = 1;

            GUILayout.Space(10);

            var c = GUI.color;
            GUI.color = Color.gray;
            GUILayout.Box(GUIContent.none, horizontalLine);
            GUI.color = c;

            GUILayout.Space(10);
        }
    }
}