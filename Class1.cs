using MelonLoader;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Linq;

[assembly: MelonInfo(typeof(GlobalTimeSyncMod.MySyncMod), "Global Time Sync Stable", "2.5.0", "Author")]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace GlobalTimeSyncMod
{
    public class MySyncMod : MelonMod
    {
        public override void OnApplicationStart() // Поздняя инициализация
        {
            MelonLogger.Msg("=== ЗАПУСК СИНХРОНИЗАЦИИ (v2.5.0) ===");

            var harmony = new HarmonyLib.Harmony("com.sync.ultimate");

            // Список целей: Класс + Часы промотки
            PatchTarget(harmony, "Panel_FireStart", 3);
            PatchTarget(harmony, "Panel_Wait", 1);
            PatchTarget(harmony, "Panel_BreakDown", 1);
            PatchTarget(harmony, "Panel_GenericProgressBar", 1); // Мастер-патч для всего остального
        }

        private void PatchTarget(HarmonyLib.Harmony h, string className, int hours)
        {
            try
            {
                // Пытаемся найти тип (класс)
                System.Type type = AccessTools.TypeByName(className)
                                ?? AccessTools.TypeByName("Il2Cpp." + className);

                if (type == null)
                {
                    MelonLogger.Warning($"[SYNC] Класс {className} не найден в памяти.");
                    return;
                }

                // Ищем ЛЮБОЙ метод, который заканчивается на "Complete" или "Finished"
                // Это решит проблему с OnFirestartComplete vs OnFireStartComplete
                MethodInfo original = type.GetMethods()
                    .FirstOrDefault(m => m.Name.EndsWith("Complete", System.StringComparison.OrdinalIgnoreCase)
                                      || m.Name.EndsWith("Finished", System.StringComparison.OrdinalIgnoreCase));

                if (original != null)
                {
                    // Создаем динамический обработчик
                    var postfix = new HarmonyMethod(typeof(MySyncMod).GetMethod(nameof(UniversalPostfix)));
                    h.Patch(original, postfix: postfix);
                    MelonLogger.Msg($"[SYNC] Успешно привязан к {className} (метод: {original.Name})");
                }
                else
                {
                    MelonLogger.Warning($"[SYNC] В классе {className} не найден метод завершения.");
                }
            }
            catch (System.Exception e)
            {
                MelonLogger.Error($"[SYNC] Ошибка при обработке {className}: {e.Message}");
            }
        }

        public override void OnUpdate()
        {
            // Наша рабочая кнопка U. 5 часов ВПЕРЕД
            if (UnityEngine.Input.GetKeyDown(KeyCode.U))
            {
                ExecuteSkip(5);
            }
        }

        public static void UniversalPostfix(MethodBase __originalMethod)
        {
            // Проверяем, что это за метод, чтобы решить, на сколько скипать
            int hours = 1;
            if (__originalMethod.DeclaringType.Name.Contains("FireStart")) hours = 3;

            MelonLogger.Msg($"[SYNC] Событие {__originalMethod.DeclaringType.Name} завершено. Сдвиг на {hours} ч.");
            ExecuteSkip(hours);
        }

        public static void ExecuteSkip(int hours)
        {
            try
            {
                uConsole.RunCommand("skip " + hours);
                MelonLogger.Msg("[SYNC] В консоль: skip " + hours);
            }
            catch { }
        }
    }
}