using System;
using LedShow.Routing;
using UnityEngine;

/// <summary>
/// Projecteur statique + 4 lyres RGBW sur 192.168.1.48 / univers 33.
/// Mapping lyre 13 CH d'apres la fiche constructeur :
/// 1 Pan, 2 Pan fine, 3 Tilt, 4 Tilt fine, 5 Speed, 6 Dimmer, 7 Strobe,
/// 8 R, 9 G, 10 B, 11 W, 12 Modes auto/voice, 13 Reset.
/// </summary>
[DisallowMultipleComponent]
public class SkiShowLighting : MonoBehaviour
{
    const string DefaultControllerIp = "192.168.1.48";
    const int DefaultUniverse = 33;
    const int LyreChannelCount = 13;

    const int StaticChannelStart = 0; // canaux 1-4 RGBW
    static readonly int[] LyreStarts = { 9, 29, 49, 69 }; // canaux 10, 30, 50, 70

    // Indices 0-based dans la plage de 13 canaux de chaque lyre.
    const int ChPan = 0;
    const int ChPanFine = 1;
    const int ChTilt = 2;
    const int ChTiltFine = 3;
    const int ChSpeed = 4;
    const int ChDimmer = 5;
    const int ChStrobe = 6;
    const int ChRed = 7;
    const int ChGreen = 8;
    const int ChBlue = 9;
    const int ChWhite = 10;
    const int ChSpecial = 11; // DOIT rester a 0 sinon mode auto / voice
    const int ChReset = 12;   // DOIT rester < 250

    [SerializeField] bool flashOnSpace = true;
    [SerializeField] float flashDuration = 0.45f;
    [SerializeField] [Range(0.1f, 1f)] float fixtureBrightness = 0.85f;

    [Tooltip("Canal 1 — rotation horizontale (0-255).")]
    [SerializeField] [Range(0, 255)] int lyrePan = 0;

    [Tooltip("Canal 3 — vertical / viser le plafond. 128 = vers le haut.")]
    [SerializeField] [Range(0, 255)] int lyreTiltUp = 128;

    [SerializeField] string controllerIp = DefaultControllerIp;
    [SerializeField] int universe = DefaultUniverse;

    ArtNetSender _sender;
    readonly byte[] _dmx = new byte[ArtNetPacket.MaxDmxLength];
    float _flashRemaining;
    bool _lightsOn;
    float _keepAliveTimer;

    // Couleurs Norway conservees pendant le flash (ApplyStaticPose ne les ecrase plus).
    readonly byte[] _lyreR = new byte[4];
    readonly byte[] _lyreG = new byte[4];
    readonly byte[] _lyreB = new byte[4];
    readonly byte[] _lyreW = new byte[4];
    byte _staticR, _staticG, _staticB, _staticW;

    public static SkiShowLighting EnsureExists()
    {
        var existing = FindAnyObjectByType<SkiShowLighting>();
        if (existing != null)
            return existing;

        var go = new GameObject("Ski Show Lighting");
        return go.AddComponent<SkiShowLighting>();
    }

    void OnEnable()
    {
        ConnectSender();
        ClearLightColors();
        WriteFullFrame();
    }

    void OnDisable()
    {
        ClearLightColors();
        WriteFullFrame();
        _sender?.Dispose();
        _sender = null;
    }

    void Update()
    {
        _keepAliveTimer += Time.unscaledDeltaTime;
        var needSend = false;

        if (_keepAliveTimer >= 0.2f)
        {
            _keepAliveTimer = 0f;
            needSend = true;
        }

        if (_flashRemaining > 0f)
        {
            _flashRemaining -= Time.unscaledDeltaTime;
            if (_flashRemaining <= 0f)
            {
                ClearLightColors();
                _lightsOn = false;
            }

            needSend = true;
        }

        if (needSend)
            WriteFullFrame();
    }

    public void FlashNorway()
    {
        if (!flashOnSpace || !isActiveAndEnabled)
            return;

        ConnectSender();

        var b = fixtureBrightness;

        // Projecteur statique : blanc via canal W uniquement (pas R+G+B).
        _staticR = 0;
        _staticG = 0;
        _staticB = 0;
        _staticW = Scale(255, b);

        // 1 canal LED a la fois = 1 faisceau uni, pas 3 spots RVB separes.
        SetLyreColor(0, Scale(255, b), 0, 0, 0);                 // rouge pur
        SetLyreColor(1, 0, 0, 0, Scale(255, b));                 // blanc pur (canal W)
        SetLyreColor(2, 0, 0, Scale(255, b), 0);                 // bleu pur
        SetLyreColor(3, Scale(255, b), 0, 0, 0);                 // rouge pur

        _flashRemaining = flashDuration;
        _lightsOn = true;
        WriteFullFrame();
    }

    void WriteFullFrame()
    {
        Array.Clear(_dmx, 0, _dmx.Length);

        _dmx[StaticChannelStart] = _staticR;
        _dmx[StaticChannelStart + 1] = _staticG;
        _dmx[StaticChannelStart + 2] = _staticB;
        _dmx[StaticChannelStart + 3] = _staticW;

        var pan = (byte)Mathf.Clamp(lyrePan, 0, 255);
        var tilt = (byte)Mathf.Clamp(lyreTiltUp, 0, 255);

        for (var i = 0; i < LyreStarts.Length; i++)
        {
            var start = LyreStarts[i];
            var on = (_lyreR[i] | _lyreG[i] | _lyreB[i] | _lyreW[i]) != 0;

            _dmx[start + ChPan] = pan;
            _dmx[start + ChPanFine] = 0;
            _dmx[start + ChTilt] = tilt;
            _dmx[start + ChTiltFine] = 0;
            _dmx[start + ChSpeed] = 0;
            _dmx[start + ChDimmer] = on ? (byte)255 : (byte)0;
            _dmx[start + ChStrobe] = 0;
            _dmx[start + ChRed] = _lyreR[i];
            _dmx[start + ChGreen] = _lyreG[i];
            _dmx[start + ChBlue] = _lyreB[i];
            _dmx[start + ChWhite] = _lyreW[i];
            _dmx[start + ChSpecial] = 0; // critique : pas de mode auto / voice
            _dmx[start + ChReset] = 0;
        }

        SendUniverse();
    }

    void SetLyreColor(int index, byte r, byte g, byte b, byte w)
    {
        _lyreR[index] = r;
        _lyreG[index] = g;
        _lyreB[index] = b;
        _lyreW[index] = w;
    }

    void ClearLightColors()
    {
        _staticR = _staticG = _staticB = _staticW = 0;
        for (var i = 0; i < 4; i++)
            SetLyreColor(i, 0, 0, 0, 0);
    }

    void SendUniverse()
    {
        if (_sender == null)
            return;

        _sender.SendUniverse((ushort)Mathf.Clamp(universe, 0, 32767), _dmx, ArtNetPacket.MaxDmxLength);
    }

    void ConnectSender()
    {
        if (_sender != null)
            return;

        try
        {
            _sender = new ArtNetSender(string.IsNullOrWhiteSpace(controllerIp) ? DefaultControllerIp : controllerIp);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"{nameof(SkiShowLighting)}: impossible de connecter {controllerIp} ({e.Message}).", this);
        }
    }

    static byte Scale(int value, float brightness)
    {
        return (byte)Mathf.Clamp(Mathf.RoundToInt(value * brightness), 0, 255);
    }
}
