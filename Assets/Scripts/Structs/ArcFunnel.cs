﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;

public struct ArcFunnel
{
    public LongnoteVisualState visualState;
    public bool isRed;
    public bool isHit;

    public ArcFunnel(LongnoteVisualState visualState, bool isRed, bool isHit)
    {
        this.visualState = visualState;
        this.isRed = isRed;
        this.isHit = isHit;
    }
}

public readonly struct ArcJudge
{
    public readonly int time;
    public readonly int rawArcIdx;
    public readonly bool isStrict;

    public ArcJudge(int time, int rawArcIdx, bool isStrict)
    {
        this.time = time;
        this.rawArcIdx = rawArcIdx;
        this.isStrict = isStrict;
    }

    public ArcJudge(ArcJudge judge, bool strict)
        : this(judge.time, judge.rawArcIdx, strict) {;}
}

public enum ArcState
{
    Normal,
    Unheld,
    Red
}

public struct ArcCompleteState
{
    public ArcState state;
    public float redRoll;
    public float cutoff;

    public const float maxRedRoll = 1;
    public const float minRedRoll = 0;
    private const float redRange = maxRedRoll - minRedRoll;

    public ArcCompleteState(ArcState state)
    {
        this.state = state;
        redRoll = 0f;
        cutoff = 0f;
    }

    public ArcCompleteState(ArcCompleteState from, ArcState withState)
    {
        state = withState;
        redRoll = from.redRoll;
        cutoff = from.cutoff;
    }

    public void Update(float currentBpm, float deltaTime = 0.02f)
    {
        const float dfac = 10;
        float timef = deltaTime * dfac;
        switch(state)
        {
            case ArcState.Normal:
            case ArcState.Unheld:
                redRoll = math.min(redRoll - timef * redRange, minRedRoll);
                return;

            case ArcState.Red:
                redRoll = math.min(redRoll + timef * redRange, maxRedRoll);
                return;
        }
    }
}

public struct NativeMatrIterator<T> : IDisposable where T: struct
{
    private NativeArray<T> contents;
    private readonly NativeArray<int> startIndices;
    private NativeArray<int> indices;

    public NativeArray<int> Indices => indices;
    public int RowCount => startIndices.Length;

    public NativeMatrIterator(T[][] matr, Allocator allocator)
    {
        indices = new NativeArray<int>(matr.Length, allocator);
        startIndices = new NativeArray<int>(matr.Length + 1, allocator);
        int midx = 0, r;

        for(r = 1; r < matr.Length; r++)
        {
            midx += matr[r-1].Length;
            startIndices[r] = midx;
        }

        //TEST
        if (r != matr.Length - 1) throw new Exception("FUCK");
        //ENDTEST

        contents = new NativeArray<T>(midx + matr[r].Length, allocator);

        for(int i = 0; i < matr.Length; i++)
        {
            for(int j = 0; j < matr[i].Length; j++)
            {
                contents[i + startIndices[i] + j] = matr[i][j];
            }
        }
    }

    public T Current(int row) => this[row, indices[row]];
    public T SetCurrent(int row, T value) => this[row, indices[row]] = value;
    public bool HasCurrent(int row) => indices[row] >= startIndices[row + 1];

    public T this[int r, int c]
    {
        get => contents[r + startIndices[r] + c];
        set => contents[r + startIndices[r] + c] = value;
    }

    public bool MoveNext(int row) => ++indices[row] < startIndices[row];

    public void Reset(int row) => indices[row] = 0;

    public void Dispose() 
    {
        contents.Dispose();
        startIndices.Dispose();
        indices.Dispose();

        GC.SuppressFinalize(this);
    }

    public bool HasNext(int row) => indices[row] < startIndices[row + 1];
    public bool HasPrev(int row) => indices[row] > startIndices[row];

    public T PeekAhead(int row, int by) => this[row, indices[row] + by];
}