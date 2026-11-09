using UnityEngine;

public sealed partial class DadSchoolPrototype
{
    public void StartQuickChallenge() { GoHome(); OpenContentChallenge(); }
    void OpenContentChallenge()
    {
        // Separate object/state: no scene load, build-settings edit or campaign reward mutation.
        if (!Home) return;
        var root = new GameObject("Dad Content Challenge");
        var challenge = root.AddComponent<DadFunLab>();
        bool slingEnabled = sling != null && sling.enabled;
        if (sling != null) sling.enabled = false;
        bool resumeMusic = music != null && music.isPlaying;
        if (music != null) music.Pause();
        enabled = false;
        challenge.Exit = () =>
        {
            challenge.enabled = false;
            enabled = true;
            if (sling != null) sling.enabled = slingEnabled;
            if (resumeMusic && music != null) music.UnPause();
            Destroy(root);
        };
    }
}
