using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class SphereTest : MonoBehaviour
{
    public Material PointCloudMat;
    object requestCountLock = new object();
    int requests = 0;
    List<Vector3> points = new List<Vector3>();
    Mesh SphereMesh;
    Thread backgroundThread;
    public bool RunPointCloud = true;
    volatile bool appRunning;
    [Range(10, 500)]
    public int delayMs = 50;
    // Start is called before the first frame update
    void Start()
    {
        appRunning = true;
        InitUnitSphere();
        ThreadedMeshCloudGeneration();
    }

    private void ThreadedMeshCloudGeneration()
    {
        var SpherePoints = SphereMesh.vertices;

        // Basic thread example to show point cloud being added and deleted
        backgroundThread = new Thread
            (() =>
            {
                while (appRunning)
                {
                    for (int i = 0; i < SpherePoints.Length; i++)
                    {
                        if (!appRunning)
                        {
                            break;
                        }
                        // software delay
                        Thread.Sleep(delayMs);
                        AddPoint(SpherePoints[i]);

                        while (!RunPointCloud) { };

                    }

                    for (int i = 0; i < SpherePoints.Length; i++)
                    {
                        if (!appRunning)
                        {
                            break;
                        }
                        // software delay
                        Thread.Sleep(delayMs);
                        DeletePoint();
                        
                        while (!RunPointCloud) { };
                    }
                }

                Debug.Log("Thread exit");

            });
        backgroundThread.Start();
    }

    private void OnApplicationQuit()
    {
        appRunning = false;
    }

    void AddPoint(Vector3 point)
    {
        lock (points) { points.Add(point); }
        lock (requestCountLock) { requests++; }
    }

    void DeletePoint()
    {
        lock (points)
        {
            if (points.Count < 1) { return; }
            points.RemoveAt(points.Count - 1);
        }
        lock (requestCountLock) { requests++; ; }
    }

    private void InitUnitSphere()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = new Vector3(1, 1, 1);
        sphere.transform.parent = transform;
        if (PointCloudMat != null) sphere.GetComponent<MeshRenderer>().material = PointCloudMat;
        SphereMesh = new Mesh();
        SphereMesh = sphere.GetComponent<MeshFilter>().mesh;
    }

    // Update is called once per frame
    void Update()
    {
        lock (requestCountLock)
        {
            if (requests > 0)
            {
                UpdateTriangles();
                requests--;
            }
            else if (requests < 0) { requests = 0; }
        }
    }

    void UpdateTriangles()
    {
        lock (points)
        {
            if (points.Count < 1) { SphereMesh.Clear(); }
            else
            {
                // shortcut buggy hack to refresh the entire mesh at once, will be problematic with large point-sets
                // todo: dump to vert/tri buffer instead?
                SphereMesh.Clear();
                SphereMesh.vertices = points.ToArray();
                SphereMesh.SetIndices(Enumerable.Range(0, points.Count).ToArray(), MeshTopology.Points, 0);
            }
        }
    }
}
