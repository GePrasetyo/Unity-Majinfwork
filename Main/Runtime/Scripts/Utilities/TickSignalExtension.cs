using System;
using System.Collections.Generic;
using UnityEngine.LowLevel;

namespace Majinfwork {
    public static partial class TickSignal {
        internal static void AddSystem<T>(Type type, PlayerLoopSystem.UpdateFunction tickDelegate) where T : struct {
            var rootLoop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < rootLoop.subSystemList.Length; i++) {
                if (rootLoop.subSystemList[i].type == typeof(T)) {
                    var category = rootLoop.subSystemList[i];
                    
                    int oldSize = category.subSystemList?.Length ?? 0;
                    var newSubsystemList = new PlayerLoopSystem[oldSize + 1];

                    if (oldSize > 0) {
                        Array.Copy(category.subSystemList, newSubsystemList, oldSize);
                    }

                    newSubsystemList[oldSize] = new PlayerLoopSystem {
                        type = type,
                        updateDelegate = tickDelegate
                    };

                    category.subSystemList = newSubsystemList;
                    rootLoop.subSystemList[i] = category;
                    PlayerLoop.SetPlayerLoop(rootLoop);
                    break;
                }
            }
        }

        internal static void RemoveSystem<T>(Type type) where T : struct {
            var rootLoop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < rootLoop.subSystemList.Length; i++) {
                if (rootLoop.subSystemList[i].type == typeof(T)) {
                    var category = rootLoop.subSystemList[i];

                    if (category.subSystemList == null) return;

                    var list = new List<PlayerLoopSystem>(category.subSystemList);
                    bool removed = false;

                    for (int j = list.Count - 1; j >= 0; j--) {
                        if (list[j].type == type) {
                            list.RemoveAt(j);
                            removed = true;
                        }
                    }

                    if (removed) {
                        category.subSystemList = list.Count > 0 ? list.ToArray() : null;
                        rootLoop.subSystemList[i] = category;
                        PlayerLoop.SetPlayerLoop(rootLoop);
                    }
                    break;
                }
            }
        }
    }
}