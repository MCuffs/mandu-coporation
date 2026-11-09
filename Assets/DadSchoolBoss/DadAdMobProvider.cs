#if !SLING_HEADLESS
using System;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;

// Android only. Official Google demo IDs are intentionally fixed for this test integration.
public sealed class DadAdMobProvider : MonoBehaviour, IDadRewardedProvider
{
    public const string TestRewardedId = "ca-app-pub-3940256099942544/5224354917";
    RewardedAd ad;
    Action<bool> completion;
    bool loading, initialized, initializing, earned;
    int requestVersion;
    float retryAt, loadedAt, retryDelay = 5;
    public bool Ready { get { return initialized && !DadAdPrivacy.Busy && ConsentInformation.CanRequestAds()
        && completion == null && ad != null && Time.realtimeSinceStartup - loadedAt < 3300 && ad.CanShowAd(); } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Application.platform != RuntimePlatform.Android) return;
        var host = new GameObject("AdMob rewarded test ads").AddComponent<DadAdMobProvider>();
        DontDestroyOnLoad(host.gameObject);
        DadRewardedAds.Bind(host);
        DadAdPrivacy.Required = () => ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
        DadAdPrivacy.Open = host.PrivacyOptions;
        host.GatherConsent();
    }
    static void Main(Action action) { MobileAdsEventExecutor.ExecuteInUpdate(action); }

    void GatherConsent()
    {
        DadAdPrivacy.Busy = true;
        ConsentInformation.Update(new ConsentRequestParameters(), error => Main(() => {
            if (error != null) { DadAdPrivacy.Busy = false; ConsentReady(); return; }
            ConsentForm.LoadAndShowConsentFormIfRequired(formError => Main(() => {
                DadAdPrivacy.Busy = false; ConsentReady();
            }));
        }));
    }
    void ConsentReady()
    {
        if (!ConsentInformation.CanRequestAds()) { retryAt = Time.realtimeSinceStartup + 60; return; }
        if (initialized) { retryAt = 0; return; }
        if (initializing) return;
        initializing = true;
        MobileAds.Initialize(status => Main(() => {
            initializing = false; initialized = status != null;
            retryAt = Time.realtimeSinceStartup + (initialized ? 0 : 60);
        }));
    }
    void PrivacyOptions()
    {
        if (completion != null) return;
        DadAdPrivacy.Busy = true;
        ConsentForm.ShowPrivacyOptionsForm(error => Main(() => {
            DadAdPrivacy.Busy = false;
            DisposeAd(); // A cached request predates the user's updated choice.
            ConsentReady();
        }));
    }
    void Update()
    {
        if (DadAdPrivacy.Busy || completion != null || loading || Time.realtimeSinceStartup < retryAt) return;
        if (!initialized) { if (!initializing) GatherConsent(); return; }
        if (!ConsentInformation.CanRequestAds()) return;
        if (ad != null && Time.realtimeSinceStartup - loadedAt >= 3300) DisposeAd();
        if (ad == null) Load();
    }
    void Load()
    {
        loading = true;
        int version = requestVersion;
        RewardedAd.Load(TestRewardedId, new AdRequest(), (loaded, error) => Main(() => {
            loading = false;
            if (this == null) { if (loaded != null) loaded.Destroy(); return; }
            if (version != requestVersion) { if (loaded != null) loaded.Destroy(); return; }
            if (error != null || loaded == null) {
                if (loaded != null) loaded.Destroy();
                retryAt = Time.realtimeSinceStartup + retryDelay;
                retryDelay = Mathf.Min(60, retryDelay * 2); return;
            }
            if (!ConsentInformation.CanRequestAds()) { loaded.Destroy(); return; }
            ad = loaded; loadedAt = Time.realtimeSinceStartup; retryDelay = 5;
            loaded.OnAdFullScreenContentClosed += () => Main(() => { if (ad == loaded) Finish(earned); });
            loaded.OnAdFullScreenContentFailed += errorInfo => Main(() => { if (ad == loaded) Finish(false); });
        }));
    }
    public void Show(Action<bool> completed)
    {
        if (!Ready) { completed(false); return; }
        completion = completed; earned = false;
        var shown = ad;
        try { shown.Show(reward => Main(() => { if (ad == shown && completion != null) earned = true; })); }
        catch { Finish(false); }
    }
    void Finish(bool rewarded)
    {
        var done = completion; completion = null;
        DisposeAd(); retryAt = Time.realtimeSinceStartup + 2;
        if (done != null) done(rewarded);
    }
    void DisposeAd() { requestVersion++; if (ad != null) { ad.Destroy(); ad = null; } }
    void OnDestroy() { DisposeAd(); }
}
#endif
