using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    public static class ActorContainer
    {
        private static List<Actor> allActors = new List<Actor>();

        public static void RegisterActor(Actor actor)
        {
            allActors.Add(actor);
        }

        public static void UnregisterActor(Actor actor)
        {
            allActors.Remove(actor);
        }

        public static Actor GetActorByGameObjectName(string gameObjectName)
        {
            Actor actor = allActors.Find(x => x.gameObject.name == gameObjectName);

            if (actor == null)
            {
                Debug.LogError("Cannot find actor by gameObjectName: " + gameObjectName + ", is it added to LevelManager?");
            }

            return actor;
        }

        public static Actor GetFrontActor(Actor.Camp camp, Actor finder, float distance)
        {
            for (int i = 0; i < allActors.Count; i++)
            {
                if (allActors[i].camp == camp)
                {
                    if (finder.IsFacingRight && allActors[i].transform.position.x > finder.transform.position.x && Mathf.Abs(allActors[i].transform.position.x - finder.transform.position.x) <= distance)
                    {
                        return allActors[i];
                    }
                    else if (!finder.IsFacingRight && allActors[i].transform.position.x < finder.transform.position.x && Mathf.Abs(allActors[i].transform.position.x - finder.transform.position.x) <= distance)
                    {
                        return allActors[i];
                    }
                }
            }

            return null;
        }

        public static List<Actor> GetAroundSameCampActors(Actor finder, float distance)
        {
            List<Actor> aroundActors = new List<Actor>();

            for (int i = 0; i < allActors.Count; i++)
            {
                if (allActors[i] == finder)
                {
                    continue;
                }

                if (allActors[i].currentHealth > 0 && allActors[i].camp == finder.camp
                    && Mathf.Abs(allActors[i].transform.position.y - finder.transform.position.y) <= 0.5f
                    && Mathf.Abs(allActors[i].transform.position.x - finder.transform.position.x) <= distance)
                {
                    aroundActors.Add(allActors[i]);
                }
            }

            return aroundActors;
        }

        public static Actor GetActorByCamp(Actor.Camp camp)
        {
            return allActors.Find(actor => actor.camp == camp);
        }

        public static List<Actor> GetActorsByCamp(Actor.Camp camp)
        {
            return allActors.FindAll(actor => actor.camp == camp);
        }

        public static Actor GetCloestOpponent(Actor finder, bool needAlive = true)
        {
            Actor.Camp opponentCamp = finder.camp == Actor.Camp.Hero ? Actor.Camp.Monster : Actor.Camp.Hero;
            Actor cloestOpponent = null;

            for (int i = 0; i < allActors.Count; i++)
            {
                if (allActors[i].camp == opponentCamp)
                {
                    if (needAlive && allActors[i].currentHealth <= 0)
                    {
                        continue;
                    }

                    if (cloestOpponent == null)
                    {
                        if (Mathf.Abs(allActors[i].transform.position.y - finder.transform.position.y) <= 0.5f) cloestOpponent = allActors[i];
                    }
                    else
                    {
                        if (Mathf.Abs(allActors[i].transform.position.x - finder.transform.position.x) < Mathf.Abs(cloestOpponent.transform.position.x - finder.transform.position.x)
                            && Mathf.Abs(allActors[i].transform.position.y - finder.transform.position.y) <= 0.5f)
                        {
                            cloestOpponent = allActors[i];
                        }
                    }
                }
            }

            return cloestOpponent;
        }

        public static Actor GetActorByInstanceID(int instanceID)
        {
            Actor actor = allActors.Find(x => x.GetInstanceID() == instanceID);

            if (actor == null)
            {
                Debug.LogError("Cannot find actor by instanceID: " + instanceID + ", is it added to LevelManager?");
            }

            return actor;
        }

        public static void ClearAll()
        {
            for (int i = 0; i < allActors.Count; i++)
            {
                UnityEngine.Object.Destroy(allActors[i].gameObject);
                i--;
            }

            allActors.Clear();
        }
    }
}