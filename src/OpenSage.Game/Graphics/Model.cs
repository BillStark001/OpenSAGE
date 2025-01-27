﻿﻿using System.IO;
using OpenSage.Content.Loaders;
using OpenSage.Data.StreamFS;
using OpenSage.Data.W3x;
using OpenSage.FileFormats;
using System.Diagnostics;
using OpenSage.Content;

namespace OpenSage.Graphics
{
    [DebuggerDisplay("Model '{Name}'")]
    public sealed class Model : BaseAsset
    {
        internal static Model ParseAsset(BinaryReader reader, Asset asset, AssetImportCollection imports)
        {
            var hierarchy = imports.GetImportedData<ModelBoneHierarchy>(reader).Value;
            var subObjects = reader.ReadArrayAtOffset(() => W3xSubObject.Parse(reader, imports));

            var modelSubObjects = new ModelSubObject[subObjects.Length];
            for (var i = 0; i < subObjects.Length; i++)
            {
                var subObject = subObjects[i];
                modelSubObjects[i] = new ModelSubObject(
                    subObject.Name,
                    subObject.Name,
                    hierarchy.Bones[subObject.BoneIndex],
                    subObject.RenderObject);
            }

            return new Model(asset, hierarchy, modelSubObjects);
        }

        public readonly ModelBoneHierarchy BoneHierarchy;
        public readonly ModelSubObject[] SubObjects;

        public readonly bool HasSkinnedMeshes;

        internal Model(
            string name,
            ModelBoneHierarchy boneHierarchy,
            ModelSubObject[] subObjects)
            : this(boneHierarchy, subObjects)
        {
            SetNameAndInstanceId("W3DContainer", name);
        }

        internal Model(
            Asset asset,
            ModelBoneHierarchy boneHierarchy,
            ModelSubObject[] subObjects)
            : this(boneHierarchy, subObjects)
        {
            SetNameAndInstanceId(asset);
        }

        private Model(
            ModelBoneHierarchy boneHierarchy,
            ModelSubObject[] subObjects)
        {
            BoneHierarchy = boneHierarchy;
            SubObjects = subObjects;

            foreach (var subObject in subObjects)
            {
                if (subObject.RenderObject is ModelMesh modelMesh && modelMesh.Skinned)
                {
                    HasSkinnedMeshes = true;
                    break;
                }
            }
        }

        internal ModelInstance CreateInstance(AssetLoadContext loadContext)
        {
            return new ModelInstance(this, loadContext);
        }
    }
}
