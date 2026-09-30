using System;
using UnityEngine;

[Serializable]
public sealed class ReputationData
{
    [SerializeField, Range(0f, 100f)] float recognition;
    [SerializeField, Range(0f, 100f)] float crewRespect;
    [SerializeField, Range(0f, 100f)] float cityImpact;

    public float Recognition => recognition;
    public float CrewRespect => crewRespect;
    public float CityImpact => cityImpact;
    public float OverallReputation => Mathf.Clamp(
        recognition * 0.35f + crewRespect * 0.30f + cityImpact * 0.35f,
        0f,
        100f);

    public ReputationData() : this(0f, 0f, 0f)
    {
    }

    public ReputationData(float recognition, float crewRespect, float cityImpact)
    {
        Set(recognition, crewRespect, cityImpact);
    }

    internal void Set(float newRecognition, float newCrewRespect, float newCityImpact)
    {
        recognition = Mathf.Clamp(newRecognition, 0f, 100f);
        crewRespect = Mathf.Clamp(newCrewRespect, 0f, 100f);
        cityImpact = Mathf.Clamp(newCityImpact, 0f, 100f);
    }
}
