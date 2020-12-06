using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class MineswapperScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public KMSelectable[] buttons;
    public Renderer[] labels;
    public Renderer[] leds;
    public Renderer[] borders;
    public Material[] mats;
    public TextMesh[] digits;
    public GameObject barycentre;

    private Vector3[] pos = new Vector3[36];
    private bool[,] minepresent = new bool[6,6];
    private int[] minecount = new int[36];
    private int[] ordering = new int[36];
    private int[] pair = new int[2] { -1, -1};
    private bool swapping;

    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private void Awake()
    {
        moduleID = ++moduleIDCounter;
        int rand = Random.Range(12, 25);
        for (int i = 0; i < rand; i++)
        {
            int r = Random.Range(0, 36);
            while (minepresent[r / 6, r % 6])
                r = Random.Range(0, 36);
            minepresent[r / 6, r % 6] = true;
        }
        for(int i = 0; i < 36; i++)
        {
            pos[i] = buttons[i].transform.localPosition;
            labels[i].material = mats[minepresent[i / 6, i % 6] ? 3 : 2];
            int[] m = new int[2] { i / 6, i % 6 };
            for (int j = -1; j < 2; j++)
                for (int k = -1; k < 2; k++)
                    if ((j != 0 || k != 0) && m[0] + j >= 0 && m[0] + j < 6 && m[1] + k >= 0 && m[1] + k < 6 && minepresent[m[0] + j, m[1] + k])
                        minecount[i]++;
            digits[i].text = minecount[i].ToString();
            ordering[i] = i;
        }
        ordering = ordering.Shuffle();
        while (Check())
            ordering = ordering.Shuffle();       
        Debug.LogFormat("[Mineswapper #{0}] Initial state:\n[Mineswapper #{0}] {1}", moduleID, string.Join("", minecount.Select((x, i) => (minepresent[Array.IndexOf(ordering, i) / 6, Array.IndexOf(ordering, i) % 6] ? "*" : " ") + minecount[Array.IndexOf(ordering, i)].ToString() + (minepresent[Array.IndexOf(ordering, i) / 6, Array.IndexOf(ordering, i) % 6] ? "*" : " ") + (i == 35 ? "" : i % 6 == 5 ? ("\n[Mineswapper #" + moduleID + "] ") : "|")).ToArray()));
        Debug.LogFormat("[Mineswapper #{0}] Possible solve state:\n[Mineswapper #{0}] {1}", moduleID, string.Join("", minecount.Select((x, i) => (minepresent[i / 6, i % 6] ? "*" : " ") + x.ToString() + (minepresent[i / 6, i % 6] ? "*" : " ") + (i == 35 ? "" : i % 6 == 5 ? ("\n[Mineswapper #" + moduleID + "] ") : "|")).ToArray()));
        for (int i = 0; i < 36; i++)
            buttons[i].transform.localPosition = pos[ordering[i]];
        foreach(KMSelectable button in buttons)
        {
            int b = Array.IndexOf(buttons, button);
            button.OnInteract += delegate () {
                if (!moduleSolved && !swapping)
                {
                    if (pair[0] == -1)
                    {
                        pair[0] = b;
                        borders[ordering[b]].material = mats[4];
                        button.transform.localPosition += new Vector3(0, 0.006f, 0);
                        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, button.transform);
                        for (int i = 0; i < 36; i++)
                            leds[i].material = mats[0];
                    }
                    else if (pair[1] == -1)
                    {
                        pair[1] = b;
                        if (pair[0] == pair[1])
                        {
                            borders[ordering[b]].material = mats[0];
                            button.transform.localPosition -= new Vector3(0, 0.006f, 0);
                            button.AddInteractionPunch(0.75f);
                            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
                            pair[0] = -1;
                            pair[1] = -1;
                            if (Check())
                            {
                                moduleSolved = true;
                                module.HandlePass();
                            }
                        }
                        else
                            StartCoroutine(Swap(pair[0], pair[1]));
                    }
                }
            return false; };
        }
    }

    private IEnumerator Swap(int a, int b)
    {
        swapping = true;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, buttons[b].transform);
        borders[ordering[b]].material = mats[4];
        float time = 0;
        float cutoff = Time.deltaTime;
        barycentre.transform.localPosition = (pos[ordering[a]] + pos[ordering[b]]) / 2;
        while(time < 0.5f - cutoff)
        {
            float del = Time.deltaTime;
            time += del;
            buttons[a].transform.RotateAround(barycentre.transform.position, transform.up, del * 360);
            buttons[b].transform.RotateAround(barycentre.transform.position, transform.up, del * 360);
            buttons[a].transform.localRotation = Quaternion.Euler(-90, 0, 90);
            buttons[b].transform.localRotation = Quaternion.Euler(-90, 0, 90);
            yield return null;
        }
        buttons[a].transform.localPosition = pos[ordering[b]];
        buttons[b].transform.localPosition = pos[ordering[a]];
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        buttons[a].AddInteractionPunch(0.375f);
        buttons[b].AddInteractionPunch(0.375f);
        borders[ordering[a]].material = mats[0];
        borders[ordering[b]].material = mats[0];
        pair[0] = -1;
        pair[1] = -1;
        int swap = 0;
        swap = ordering[a];
        ordering[a] = ordering[b];
        ordering[b] = swap;
        {
            moduleSolved = true;
            module.HandlePass();
        }
        swapping = false;
    }

    private bool Check()
    {
        bool[][] correct = new bool[6][] { new bool[6], new bool[6], new bool[6], new bool[6], new bool[6], new bool[6]};
        for(int i = 0; i < 36; i++)
        {
            int minecheck = 0;
            int[] m = new int[2] { i / 6, i % 6 };
            for (int j = -1; j < 2; j++)
                for (int k = -1; k < 2; k++) {
                    if ((j != 0 || k != 0) && m[0] + j >= 0 && m[0] + j < 6 && m[1] + k >= 0 && m[1] + k < 6)
                    {
                        int get = Array.IndexOf(ordering, (m[0] + j) * 6 + m[1] + k);
                        if(minepresent[get / 6, get % 6])
                           minecheck++;
                    }
                }           
            correct[m[0]][m[1]] = minecheck == minecount[Array.IndexOf(ordering, i)];
            leds[Array.IndexOf(ordering, i)].material = mats[minecheck == minecount[Array.IndexOf(ordering, i)] ? 1 : 0];
        }
        return !correct.Any(x => x.Contains(false));
    }
}
