// ╔══════════════════════════════════════════════════════════════╗
// ║  FACE CULLING — COPIER-COLLER                                ║
// ║                                                              ║
// ║  Théorie :                                                   ║
// ║  Chaque bloc a 6 faces (haut, bas, gauche, droite,           ║
// ║  devant, derrière). Si un voisin est collé contre une face,  ║
// ║  cette face est invisible — inutile de la dessiner !         ║
// ║                                                              ║
// ║  Ce script regarde les 6 voisins de chaque bloc et retourne  ║
// ║  uniquement les faces qui sont exposées à l'air.             ║
// ╚══════════════════════════════════════════════════════════════╝

using System.Collections.Generic;
using UnityEngine;

public enum FaceDirection { Top, Bottom, Right, Left, Front, Back }

public static class FaceCuller
{
    // Les 6 directions possibles autour d'un bloc
    public static readonly Vector3Int[] DirectionVectors =
    {
        Vector3Int.up,               // Top
        Vector3Int.down,             // Bottom
        Vector3Int.right,            // Right
        Vector3Int.left,             // Left
        new Vector3Int(0, 0,  1),    // Front
        new Vector3Int(0, 0, -1),    // Back
    };

    // Retourne les faces visibles d'un bloc (celles qui ne sont pas cachées par un voisin)
    public static List<FaceDirection> GetVisibleFaces(Vector3Int pos, Dictionary<Vector3Int, BlockType> blocks)
    {
        var visible = new List<FaceDirection>();

        for (int i = 0; i < 6; i++)
        {
            Vector3Int neighborPos = pos + DirectionVectors[i];

            bool neighborIsSolid = blocks.TryGetValue(neighborPos, out var neighbor)
                                   && neighbor.IsSolid();

            // Si le voisin n'est pas solide → la face est exposée → on la garde
            if (!neighborIsSolid)
                visible.Add((FaceDirection)i);
        }

        return visible;
    }
}