#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class QuickGraffitiTests
{
    readonly List<Object> objects = new List<Object>();
    GameObject Create(string name) { var g = new GameObject(name); objects.Add(g); return g; }
    [TearDown] public void Cleanup()
    {
        foreach (var obj in objects) if (obj != null) Object.DestroyImmediate(obj);
        objects.Clear();
    }

    Material Material => AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Graffiti/QuickGraffitiReveal.mat");
    RaycastHit Wall()
    {
        var wall = Create("Quick graffiti test wall");
        wall.transform.position = new Vector3(500f, 1200f, 1000f);
        var collider = wall.AddComponent<BoxCollider>();
        collider.size = new Vector3(8f, 8f, 0.1f);
        Physics.SyncTransforms();
        Assert.IsTrue(collider.Raycast(new Ray(wall.transform.position - Vector3.forward * 3f, Vector3.forward), out RaycastHit hit, 4f));
        return hit;
    }

    [Test] public void CatalogContainsOnlyTheTwoHologramsAndTwelveTrimmedGraffitis()
    {
        var catalog = QuickGraffitiBookBuilder.LoadCatalog();
        Assert.That(catalog.Length, Is.EqualTo(14));
        Assert.That(catalog.Select(e=>e.sprite).Distinct().Count(), Is.EqualTo(14));
        Assert.That(catalog.Count(e=>e.rotateArtwork), Is.EqualTo(12));
        foreach (var entry in catalog)
        {
            Assert.That(entry.sprite, Is.Not.Null);
            Assert.That(entry.sprite.name, Is.Not.EqualTo("Blackbook_StickersAtlas_0"));
            Assert.That(entry.Size.x, Is.GreaterThan(0.5f));
            Assert.That(entry.Size.y, Is.LessThan(entry.Size.x * 1.1f));
        }
    }

    [Test] public void PlacementRejectsEdgesAndHorizontalSurfaces()
    {
        RaycastHit hit = Wall();
        var entry = QuickGraffitiBookBuilder.LoadCatalog()[2];
        Assert.IsTrue(GraffitiQuickPainter.CanPlace(hit, entry));
        hit.point += Vector3.right * 3.7f;
        Assert.IsFalse(GraffitiQuickPainter.CanPlace(hit, entry));
        hit.normal = Vector3.up;
        Assert.IsFalse(GraffitiQuickPainter.CanPlace(hit, entry));
    }

    [Test] public void ReleasingPaintPausesAndClickingSamePieceResumesIt()
    {
        var go = Create("Quick graffiti test player");
        go.AddComponent<CharacterController>();
        var fps = go.AddComponent<FPSController>(); fps.enabled = false;
        var painter = go.AddComponent<GraffitiPainter>(); painter.enabled = false;
        var quick = go.AddComponent<GraffitiQuickPainter>(); quick.enabled = false;
        quick.player = fps; quick.painter = painter;
        quick.library = QuickGraffitiBookBuilder.LoadCatalog(); quick.artworkMaterial = Material;
        typeof(GraffitiQuickPainter).GetField("<IsQuickMode>k__BackingField", BindingFlags.Instance|BindingFlags.NonPublic).SetValue(quick, true);
        RaycastHit hit = Wall();
        Assert.IsTrue(quick.TryBegin(hit));
        var piece = quick.CurrentPiece;
        Assert.That(piece.Progress, Is.Zero);
        quick.Advance(0.5f);
        float partial = piece.Progress;
        Assert.That(partial, Is.GreaterThan(0f).And.LessThan(1f));
        quick.StopStroke();
        quick.Advance(4f);
        Assert.That(piece.Progress, Is.EqualTo(partial));
        Assert.IsTrue(quick.TryBegin(hit));
        Assert.That(quick.CurrentPiece, Is.SameAs(piece));
        quick.Advance(0.5f);
        Assert.That(piece.Progress, Is.EqualTo(partial * 2f).Within(0.001f));
    }

    [Test] public void SprayPathAlternatesDirectionsAcrossTheArtwork()
    {
        Vector2 firstStart = QuickGraffitiPiece.SweepUV(0f);
        Vector2 firstEnd = QuickGraffitiPiece.SweepUV(0.124f);
        Vector2 secondStart = QuickGraffitiPiece.SweepUV(0.126f);
        Vector2 secondEnd = QuickGraffitiPiece.SweepUV(0.249f);
        Assert.That(firstEnd.x, Is.GreaterThan(firstStart.x));
        Assert.That(secondEnd.x, Is.LessThan(secondStart.x));
        Assert.That(secondStart.y, Is.LessThan(firstStart.y));
    }

    GraffitiQuickPainter SizeTestPlayer()
    {
        var go = Create("Resizable graffiti test player");
        go.AddComponent<CharacterController>();
        var fps = go.AddComponent<FPSController>(); fps.enabled = false; fps.EnterGraffitiMode();
        var painter = go.AddComponent<GraffitiPainter>(); painter.enabled = false; painter.fps = fps;
        var quick = go.AddComponent<GraffitiQuickPainter>(); quick.enabled = false;
        quick.player = fps; quick.painter = painter; quick.library = QuickGraffitiBookBuilder.LoadCatalog();
        quick.artworkMaterial = Material;
        typeof(GraffitiQuickPainter).GetField("<IsQuickMode>k__BackingField", BindingFlags.Instance|BindingFlags.NonPublic).SetValue(quick,true);
        return quick;
    }

    [Test] public void WheelResizesWithinBoundsAndLeavesDepositedArtworkUnchanged()
    {
        var quick = SizeTestPlayer();
        var hit = Wall();
        Assert.IsTrue(quick.TryBegin(hit));
        var deposited = quick.CurrentPiece;
        quick.Advance(0.2f);
        Assert.IsFalse(quick.AdjustSize(1f), "Do not distort a piece while spraying it.");
        quick.StopStroke();
        Assert.IsTrue(quick.AdjustSize(1f));
        Assert.That(quick.SelectedSizeMultiplier, Is.GreaterThan(1f));
        Assert.That(deposited.SizeMultiplier, Is.EqualTo(1f));
        quick.AdjustSize(1000f);
        Assert.That(quick.SelectedSizeMultiplier, Is.EqualTo(quick.sizeMultiplierRange.y));
        quick.AdjustSize(-1000f);
        Assert.That(quick.SelectedSizeMultiplier, Is.EqualTo(quick.sizeMultiplierRange.x));
    }

    [Test] public void PreviewSizeAndSprayPathUseTheSameScaledDimensions()
    {
        var entry = QuickGraffitiBookBuilder.LoadCatalog()[2];
        Vector2 original = entry.Size;
        var piece = Create("Scalable preview test").AddComponent<QuickGraffitiPiece>();
        piece.Initialize(entry,Material,Wall(),true);
        piece.SetPreviewSize(1.75f);
        Vector3 bounds = piece.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        Assert.That(bounds.x,Is.EqualTo(original.x*1.75f).Within(0.001f));
        Assert.That(bounds.y,Is.EqualTo(original.y*1.75f).Within(0.001f));
        Vector3 localSweep = piece.transform.InverseTransformPoint(piece.SweepWorldPoint());
        Assert.That(localSweep.x,Is.EqualTo(-bounds.x*0.5f).Within(0.001f));
        Assert.That(entry.Size,Is.EqualTo(original),"Resizing must not change the shared catalog entry.");
    }

    [Test] public void ScaledPlacementStillRejectsWallEdges()
    {
        var hit = Wall(); hit.point += Vector3.right*2.2f;
        var entry = QuickGraffitiBookBuilder.LoadCatalog()[2];
        Assert.IsTrue(GraffitiQuickPainter.CanPlace(hit,entry,1f));
        Assert.IsFalse(GraffitiQuickPainter.CanPlace(hit,entry,2f));
    }

    [Test] public void FreeModeHintKeepsEditableTextAndOnlyAppearsDuringFreeGraffiti()
    {
        var quick = SizeTestPlayer();
        var hint = Create("Editable free graffiti hint");
        var text = hint.AddComponent<TMPro.TextMeshProUGUI>(); text.text="Mi texto personalizado de E";
        quick.freeModeHint = hint;
        quick.RefreshPreview(); Assert.IsFalse(hint.activeSelf);
        quick.SetQuickMode(false); quick.RefreshPreview(); Assert.IsTrue(hint.activeSelf);
        quick.painter.menuOpen=true; quick.RefreshPreview(); Assert.IsFalse(hint.activeSelf);
        quick.painter.menuOpen=false; quick.player.ExitGraffitiMode(); quick.RefreshPreview(); Assert.IsFalse(hint.activeSelf);
        Assert.That(text.text,Is.EqualTo("Mi texto personalizado de E"));
    }

    [Test] public void GraffitiModeRestoresEachHintsPreviousVisibility()
    {
        var go = Create("Hint visibility test player"); go.AddComponent<CharacterController>();
        var fps = go.AddComponent<FPSController>(); fps.enabled = false;
        var visible = Create("Originally visible hint");
        var hidden = Create("Originally hidden hint"); hidden.SetActive(false);
        var visibility = go.AddComponent<GraffitiModeHudVisibility>(); visibility.enabled = false;
        visibility.player = fps; visibility.hideWhilePainting = new[] { visible, hidden };
        fps.EnterGraffitiMode(); visibility.Refresh();
        Assert.IsFalse(visible.activeSelf); Assert.IsFalse(hidden.activeSelf);
        fps.ExitGraffitiMode(); visibility.Refresh();
        Assert.IsTrue(visible.activeSelf); Assert.IsFalse(hidden.activeSelf);
    }

    [Test] public void RevealPaintsPassesRatherThanFadingTheWholeImage()
    {
        var texture = new Texture2D(2,2); objects.Add(texture);
        texture.SetPixels(new[] { Color.white,Color.white,Color.white,Color.white }); texture.Apply();
        var sprite = Sprite.Create(texture,new Rect(0,0,2,2),Vector2.one*0.5f);objects.Add(sprite);
        var entry = new QuickGraffitiEntry { sprite=sprite,width=2f,paintSeconds=4f };
        var hit = Wall();
        var piece = Create("Reveal test piece").AddComponent<QuickGraffitiPiece>();
        piece.Initialize(entry,Material,hit);
        piece.gameObject.layer=31;
        var cam = Create("Reveal test camera").AddComponent<Camera>(); cam.enabled=false;cam.cullingMask=1<<31;
        cam.transform.position=piece.transform.position+hit.normal*3f;
        cam.transform.LookAt(piece.transform.position,Vector3.up);
        cam.orthographic=true;cam.orthographicSize=1.1f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.clear;
        var target=new RenderTexture(64,64,16);objects.Add(target);cam.targetTexture=target;
        var readback=new Texture2D(64,64,TextureFormat.RGBA32,false);objects.Add(readback);
        RenderTexture previous=RenderTexture.active;
        try
        {
            piece.Advance(1f);cam.Render();RenderTexture.active=target;
            readback.ReadPixels(new Rect(0,0,64,64),0,0);readback.Apply();
            Assert.That(readback.GetPixel(32,57).a,Is.GreaterThan(0.5f));
            Assert.That(readback.GetPixel(32,10).a,Is.LessThan(0.01f));
            piece.Advance(4f);cam.Render();RenderTexture.active=target;
            readback.ReadPixels(new Rect(0,0,64,64),0,0);readback.Apply();
            Assert.That(readback.GetPixel(32,10).a,Is.GreaterThan(0.95f));
            Assert.IsTrue(piece.IsComplete);
        }
        finally { RenderTexture.active=previous;cam.targetTexture=null; }
    }
}
#endif
