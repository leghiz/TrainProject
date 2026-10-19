using System;
using UnityEngine;

public struct TooltipContent
{
    public string Title;
    public string Info;
    public int Hp;
    public int MaxHp;

    public bool HasHp => MaxHp > 0;
}

public class HoverModel
{
    public object Owner { get; private set; }
    public Func<TooltipContent> Read { get; private set; }
    public Vector3 Anchor { get; private set; }
    public bool HasTarget => Read != null;

    public event Action Changed;

    public void Show(object owner, Func<TooltipContent> read, Vector3 anchor)
    {
        Owner = owner;
        Read = read;
        Anchor = anchor;
        Changed?.Invoke();
    }
    
    public void Clear(object owner)
    {
        if (Owner != owner) return;
        Owner = null;
        Read = null;
        Changed?.Invoke();
    }
}
