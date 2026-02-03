using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class SixSevenMatch3Tests
{
    private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
    private readonly List<Texture2D> textures = new List<Texture2D>();

    [TearDown]
    public void TearDown()
    {
        foreach (var obj in created)
        {
            if (obj != null)
                UnityEngine.Object.DestroyImmediate(obj);
        }
        created.Clear();

        foreach (var tex in textures)
        {
            if (tex != null)
                UnityEngine.Object.DestroyImmediate(tex);
        }
        textures.Clear();
    }

    [Test]
    public void MatchDetection_Works()
    {
        var manager = CreateManager(7, 7);
        var tiles = CreateTiles(manager, 7, 7);
        SetPrivate(manager, "tiles", tiles);

        // Clear board with no matches.
        FillNoMatches(tiles);

        // Force a horizontal match at y=3, x=2..4
        tiles[2, 3].Type = 1;
        tiles[3, 3].Type = 1;
        tiles[4, 3].Type = 1;

        bool[,] matches = (bool[,])InvokePrivate(manager, "FindMatches");

        Assert.IsTrue(matches[2, 3]);
        Assert.IsTrue(matches[3, 3]);
        Assert.IsTrue(matches[4, 3]);
    }

    [Test]
    public void SwapWithoutMatch_Reverts()
    {
        var manager = CreateManager(7, 7);
        var tiles = CreateTiles(manager, 7, 7);
        SetPrivate(manager, "tiles", tiles);
        FillNoMatches(tiles);

        var a = tiles[0, 0];
        var b = tiles[1, 0];
        int aType = a.Type;
        int bType = b.Type;

        IEnumerator swap = (IEnumerator)InvokePrivate(manager, "HandleSwap", a, b);
        while (swap.MoveNext()) { }

        Assert.AreEqual(aType, a.Type);
        Assert.AreEqual(bType, b.Type);
    }

    [Test]
    public void ScoringIncrements_PerTile()
    {
        var manager = CreateManager(7, 7);
        var tiles = CreateTiles(manager, 7, 7);
        SetPrivate(manager, "tiles", tiles);
        FillNoMatches(tiles);

        tiles[0, 0].Type = 2;
        tiles[1, 0].Type = 2;
        tiles[2, 0].Type = 2;

        int cleared = (int)InvokePrivate(manager, "ClearMatches");
        int score = (int)GetPrivate(manager, "score");

        Assert.AreEqual(3, cleared);
        Assert.AreEqual(30, score);
    }

    [Test]
    public void MovesDecrement_OncePerAttempt()
    {
        var manager = CreateManager(7, 7);
        var tiles = CreateTiles(manager, 7, 7);
        SetPrivate(manager, "tiles", tiles);
        FillNoMatches(tiles);

        SetPrivate(manager, "moves", 30);

        var a = tiles[0, 0];
        var b = tiles[1, 0];

        IEnumerator swap = (IEnumerator)InvokePrivate(manager, "HandleSwap", a, b);
        while (swap.MoveNext()) { }

        int moves = (int)GetPrivate(manager, "moves");
        Assert.AreEqual(29, moves);
    }

    private GameManager CreateManager(int width, int height)
    {
        var go = new GameObject("GameManager");
        created.Add(go);
        var manager = go.AddComponent<GameManager>();

        SetPrivate(manager, "width", width);
        SetPrivate(manager, "height", height);
        SetPrivate(manager, "scorePerTile", 10);
        SetPrivate(manager, "sprites", CreateSprites(3));

        return manager;
    }

    private TileView[,] CreateTiles(GameManager manager, int width, int height)
    {
        var tiles = new TileView[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var go = new GameObject($"Tile_{x}_{y}");
                created.Add(go);
                var tile = go.AddComponent<TileView>();
                tile.Init(manager, x, y);
                tiles[x, y] = tile;
            }
        }
        return tiles;
    }

    private void FillNoMatches(TileView[,] tiles)
    {
        int width = tiles.GetLength(0);
        int height = tiles.GetLength(1);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                tiles[x, y].Type = (x + y) % 3;
            }
        }
    }

    private Sprite[] CreateSprites(int count)
    {
        var sprites = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            var tex = new Texture2D(2, 2);
            textures.Add(tex);
            sprites[i] = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            created.Add(sprites[i]);
        }
        return sprites;
    }

    private static object InvokePrivate(object target, string methodName, params object[] args)
    {
        MethodInfo mi = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (mi == null)
            throw new MissingMethodException(target.GetType().Name, methodName);
        return mi.Invoke(target, args);
    }

    private static void SetPrivate(object target, string fieldName, object value)
    {
        FieldInfo fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (fi == null)
            throw new MissingFieldException(target.GetType().Name, fieldName);
        fi.SetValue(target, value);
    }

    private static object GetPrivate(object target, string fieldName)
    {
        FieldInfo fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (fi == null)
            throw new MissingFieldException(target.GetType().Name, fieldName);
        return fi.GetValue(target);
    }
}
