using Content.Server.Storage.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Storage.EntitySystems
{
    public sealed class SpawnItemsOnUseSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpawnItemsOnUseComponent, UseInHandEvent>(OnUseInHand);
        }

        private void OnUseInHand(EntityUid uid, SpawnItemsOnUseComponent component, UseInHandEvent args)
        {
            if (args.Handled)
                return;

            var alreadySpawnedGroups = new List<string>();
            EntityUid? entityToPlaceInHands = null;
            foreach (var storageItem in component.Items)
            {
                if (!string.IsNullOrEmpty(storageItem.GroupId) &&
                    alreadySpawnedGroups.Contains(storageItem.GroupId)) continue;

                if (storageItem.SpawnProbability != 1f &&
                    !_random.Prob(storageItem.SpawnProbability))
                {
                    continue;
                }

                for (var i = 0; i < storageItem.Amount; i++)
                {
                    entityToPlaceInHands = EntityManager.SpawnEntity(storageItem.PrototypeId, EntityManager.GetComponent<TransformComponent>(args.User).Coordinates);
                }

                if (!string.IsNullOrEmpty(storageItem.GroupId)) alreadySpawnedGroups.Add(storageItem.GroupId);
            }

            if (component.Sound != null)
                SoundSystem.Play(Filter.Pvs(uid), component.Sound.GetSound(), uid);

            component.Uses--;
            if (component.Uses == 0)
            {
                args.Handled = true;
                EntityManager.DeleteEntity(uid);
            }

            if (entityToPlaceInHands != null)
            {
                _handsSystem.PickupOrDrop(args.User, entityToPlaceInHands.Value);
            }
        }
    }
}
