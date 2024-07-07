using BlockEntities.Implementations;
using Chatting;
using ModLoaderInterfaces;
using Monsters;
using NPC;
using System.Collections.Generic;

namespace CS_ColonyPoints
{
  [ModLoader.ModManager]
  public class CS_ZombieKills
  {
    //Holds the points for each zombie type
    private readonly Dictionary<ushort, double> _zombiePoints = new Dictionary<ushort, double>();

    private ConfigReader _configReader;
    private CS_ModController _modController;

    private int _rewardTrigger;
    private int _bannerDistance;
    private bool _chatNotifications;
    
    /*
     * Initialize the zombie kill mod part
     */
    public void Initialize(CS_ModController modController, ConfigReader configReader)
    {
      _modController = modController;
      _configReader = configReader;
      
      _modController.SendChatDebugNotification("MOD: EarnColonyPoint: ZombieKills: Initialize");

      MakeZombiePoints(_configReader.GetString("zombie01ID"), _configReader.GetInt("zombie01Points"));
      MakeZombiePoints(_configReader.GetString("zombie02ID"), _configReader.GetInt("zombie02Points"));
      MakeZombiePoints(_configReader.GetString("zombie03ID"), _configReader.GetInt("zombie03Points"));
      MakeZombiePoints(_configReader.GetString("zombie04ID"), _configReader.GetInt("zombie04Points"));
      MakeZombiePoints(_configReader.GetString("zombie05ID"), _configReader.GetInt("zombie05Points"));
      MakeZombiePoints(_configReader.GetString("zombie06ID"), _configReader.GetInt("zombie06Points"));
      
      _rewardTrigger = _configReader.GetInt("rewardTrigger");
      _bannerDistance = _configReader.GetInt("bannerDistance");
      _chatNotifications = _configReader.GetBool("chatNotifications");
    }

    /*
     * Make a new zombie entry
     */
    private void MakeZombiePoints(string id, int points)
    {
      _modController.SendChatDebugNotification($"MOD: EarnColonyPoint: ZombieKills: MakeZombiePoints for {id} with value {points}");

      NPCType npcType = NPCType.NPCTypesByKeyName[id];
      int type = npcType.Type;
      _zombiePoints.Add((ushort)type, points);
    }

    /*
     * Triggers when a monster dies
     */
    public void OnMonsterKill(IMonster monster)
    {
      _modController.SendChatDebugNotification("MOD: EarnColonyPoint: ZombieKills: OnMonsterDied");

      //Check if monster is close to a banner
      ServerManager.BlockEntityTracker.BannerTracker.TryGetClosest(monster.Position, out BannerTracker.Banner banner,
        _bannerDistance);
      if (banner == null)
      {
        return;
      }

      double pointsReward = GetZombiePoints(monster);

      ColonyGroupID colonyGroupId = banner.Colony.ColonyGroup.ColonyGroupID;

      // Check if we are rewarding on kill
      if (_rewardTrigger == 1)
      {
        RewardOnKill(colonyGroupId, pointsReward);

      }
      //Rewarding each day, add points to the colony
      else
      {
        double colonyPoints = GetColonyPoints(colonyGroupId);

        double updatedColonyPoints = colonyPoints + pointsReward;
        _modController.ColonyPoints[colonyGroupId] = updatedColonyPoints;
      }
    }

    /*
     * Get the points for the given zombie
     */
    private double GetZombiePoints(IMonster monster)
    {
      _modController.SendChatDebugNotification("MOD: EarnColonyPoint: ZombieKills: GetZombiePoints");
      NPCType npcType = monster.NPCType;
      int type = npcType.Type;
      _zombiePoints.TryGetValue((ushort)type, out double points);

      return points;
    }

    /*
     * Reward the colony when killing a zombie
     */
    private void RewardOnKill(ColonyGroupID colonyGroupId, double pointsReward)
    {
      _modController.SendChatDebugNotification("MOD: EarnColonyPoint: ZombieKills: RewardOnKill");
      ColonyGroup colonyGroup = ServerManager.ColonyTracker.Get(colonyGroupId);
      _modController.UpdateColonyPoints(colonyGroup, pointsReward, pointsReward);

      if (!_chatNotifications)
      {
        return;
      }

      string chatNotification = $"{colonyGroup.Name} has received {pointsReward} Colony Points for killing a zombie.";
      Chat.Send(colonyGroup.Owners, chatNotification, (EChatSendOptions)3);
    }

    /*
     * Get the current Colony Points for the colony
     */
    private double GetColonyPoints(ColonyGroupID colonyGroupId)
    {
      _modController.ColonyPoints.TryGetValue(colonyGroupId, out double points);

      return points;
    }
  }
}