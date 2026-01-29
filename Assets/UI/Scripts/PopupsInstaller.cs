using UnityEngine;
using Zenject;

public class PopupsInstaller : MonoInstaller
{
    [SerializeField] private PopupBehaviour _defaultPopup;
    [SerializeField] private PopupBehaviour _premiumPopup;

    public override void InstallBindings()
    {
        Container.Bind<PopupBehaviour>().WithId("DefaultPopup").FromInstance(_defaultPopup).AsCached();
        Container.Bind<PopupBehaviour>().WithId("PremiumPopup").FromInstance(_premiumPopup).AsCached();
    }
}
