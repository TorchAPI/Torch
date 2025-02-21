using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Engine.Platform;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Torch.API;
using Torch.API.Session;
using VRage;

namespace Torch.Patches
{
    /// <summary>
    /// A copy of <see cref="MyAsyncSaving"/> except with C# async support.
    /// </summary>
    public static class TorchAsyncSaving
    {
        /// <summary>
        /// Saves the game asynchronously
        /// </summary>
        /// <param name="torch">Torch instance</param>
        /// <param name="timeoutMs">time in milliseconds before the save is treated as failed, or -1 to wait forever</param>
        /// <param name="newSaveName">New save name, or null for current name</param>
        /// <returns>Async result of save operation</returns>
        public static Task<GameSaveResult> Save(ITorchBase torch, int timeoutMs = -1, string newSaveName = null)
        {
            Task<GameSaveResult> task = SaveInternal(torch, newSaveName);
            if (timeoutMs == -1)
                return task;
            return Task.Run(() =>
            {
                // ReSharper disable once ConvertIfStatementToReturnStatement
                if (timeoutMs >= 0 && !task.IsCompleted && !task.Wait(timeoutMs))
                    return GameSaveResult.TimedOut;
                return task.Result;
            });
        }

        private static Task<GameSaveResult> SaveInternal(ITorchBase torch, string newSaveName)
        {
            if (!MySandboxGame.IsGameReady)
                return Task.FromResult(GameSaveResult.GameNotReady);

            var saveTaskSource = new TaskCompletionSource<GameSaveResult>();
            torch.Invoke(() =>
            {
                bool snapshotSuccess = MySession.Static.Save(out MySessionSnapshot tmpSnapshot, newSaveName);
                if (!snapshotSuccess)
                {
                    saveTaskSource.SetResult(GameSaveResult.FailedToTakeSnapshot);
                    return;
                }

                if (!Game.IsDedicated)
                    TakeSaveScreenshot();
                tmpSnapshot.SaveParallel(null, null, () =>
                {
                    if (!Game.IsDedicated && MySession.Static != null)
                        ShowWorldSaveResult(tmpSnapshot.SavingSuccess);
                    saveTaskSource.TrySetResult(tmpSnapshot.SavingSuccess ? GameSaveResult.Success : GameSaveResult.FailedToSaveToDisk);
                });
            });
            return saveTaskSource.Task;
        }

        private static void ShowWorldSaveResult(bool success)
        {
            if (success)
            {
                var myHudNotification = new MyHudNotification(MyCommonTexts.WorldSaved);
                myHudNotification.SetTextFormatArguments(MySession.Static.Name);
                MyHud.Notifications.Add(myHudNotification);
            }
            else
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error,
                    MyMessageBoxButtonsType.OK,
                    new StringBuilder().AppendFormat(MyTexts.GetString(MyCommonTexts.WorldNotSaved),
                        MySession.Static.Name), MyTexts.Get(MyCommonTexts.MessageBoxCaptionError), null, null, null,
                    null, null, 0, MyGuiScreenMessageBox.ResultEnum.YES, true, null));
            }
        }

        private static void TakeSaveScreenshot()
        {
            string thumbPath = MySession.Static.ThumbPath;
            try
            {
                if (File.Exists(thumbPath))
                {
                    File.Delete(thumbPath);
                }

                MyGuiSandbox.TakeScreenshot(1200, 672, thumbPath, true, false);
            }
            catch (Exception ex)
            {
                MySandboxGame.Log.WriteLine("Could not take session thumb screenshot. Exception:");
                MySandboxGame.Log.WriteLine(ex);
            }
        }
    }
}