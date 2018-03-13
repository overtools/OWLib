using TankLib.STU.DataTypes;

namespace TankLib.STU.Types {
    
    // TODO: REWRITE, FOR TESTING ONLY
    [STU(0x5207484B, "STUModelLook")]
    public class STUModelLook : STUInstance {
        [STUField(0xBAFDAFBA, "m_materials", typeof(InlineInstanceFieldReader))]
        public STUModelMaterial[] Materials;
        
        [STUField(0x33DA887B)]
        public teStructuredDataAssetRef<ulong>[] m_33DA887B;  // STU_CBD8CDF3, old=STU_875E571C
        
        [STUField(0x05692DC5, typeof(InlineInstanceFieldReader))]
        public STUAnimationPermutation[] AnimationPermutations;

        [STUField(0x7B5D8241)]
        public teStructuredDataAssetRef<ulong> MaterialEffect;  // STUMaterialEffect

        [STUField(0xC03306D7)]
        public teStructuredDataAssetRef<ulong>[] ModelReferences;  // STUModel

        [STUField(0x312C5F1A, "m_materialEffects", typeof(InlineInstanceFieldReader))]
        public STUMaterialEffect[] MaterialEffects;
    }
    
    [STU(0x494B66C1, "STUModelMaterial")]
    public class STUModelMaterial : STUInstance {
        [STUField(0x33E51FDC, "m_material")]
        public teStructuredDataAssetRef<ulong> Material;

        [STUField(0xDC05EA3B)]
        public ulong ID;
    }
    
    [STU(0xA6DD7672, "STUAnimationPermutation")]
    public class STUAnimationPermutation : STUInstance {
        [STUField(0x3F5B86A4, "m_animation")]
        public teStructuredDataAssetRef<ulong> Animation;  // STUAnimation

        [STUField(0xAFC41FBA)]
        public float m_AFC41FBA;
    }
    
    [STU(0x4D03ED2B, "STUMaterialEffect")]
    public class STUMaterialEffect : STUInstance {
        [STUField(0x0BCD10D6, "m_materialEffect")]
        public teStructuredDataAssetRef<ulong> MaterialEffect;  // STUMaterialEffect

        [STUField(0xBAFDAFBA, "m_materials", typeof(InlineInstanceFieldReader))]
        public STUModelMaterial[] Materials;
    }
}