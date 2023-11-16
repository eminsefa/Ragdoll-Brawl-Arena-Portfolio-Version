using Fusion;
using Zenject;

namespace Project.Scripts.Managers
{
    public class ProjectMonoInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<GameManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<LevelManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<PoolManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<InputManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<UIManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<AudioManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<AnalyticsManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<ResourcesManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<NetworkRunner>().FromComponentInHierarchy().AsSingle().NonLazy();
        }
    }
}