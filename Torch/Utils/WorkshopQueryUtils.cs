using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox.Engine.Networking;
using VRage.Game;
using VRage.GameServices;

namespace Torch.Utils;

public static class WorkshopQueryUtils
{
    // TODO: Maybe later
    public static async Task<List<MyWorkshopItem>> GetModsInfo(IEnumerable<MyObjectBuilder_Checkpoint.ModItem> mods)
    {
        throw new NotImplementedException();
        /*return (await Task.WhenAll(mods.GroupBy(b => b.PublishedServiceName)
                .Select(b => GetModsInfo(b.Key, b.Select(c => c.PublishedFileId)))))
            .SelectMany(b => b).ToList();*/
    }

    public static Task<MyWorkshopItem> GetModInfo(MyObjectBuilder_Checkpoint.ModItem item)
    {
        throw new NotImplementedException();
        /*var query = MyGameService.CreateWorkshopQuery(item.PublishedServiceName);
        query.ItemIds = new() {item.PublishedFileId};
        var source = new TaskCompletionSource<MyWorkshopItem>();

        query.QueryCompleted += result =>
        {
            if (result == MyGameServiceCallResult.OK)
            {
                source.SetResult(query.Items[0]);
                return;
            }

            source.SetException(new Exception($"Workshop query resulted in {result}"));
        };
        
        query.Run();
        return source.Task;*/
    }

    private static Task<List<MyWorkshopItem>> GetModsInfo(string serviceName, IEnumerable<ulong> ids)
    {
        var query = MyGameService.CreateWorkshopQuery(serviceName);
        query.ItemIds = ids.ToList();
        var source = new TaskCompletionSource<List<MyWorkshopItem>>();

        query.QueryCompleted += result =>
        {
            if (result == MyGameServiceCallResult.OK)
            {
                source.SetResult(query.Items);
                return;
            }

            source.SetException(new Exception($"Workshop query resulted in {result}"));
        };
        
        query.Run();
        return source.Task;
    }
}