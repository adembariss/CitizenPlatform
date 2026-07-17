namespace CitizenPlatform.Domain.Enums;

/// <summary>
/// Belediye dışındaki hizmet kurumu türleri. Belediyeler ayrı <c>Municipality</c> varlığında
/// tutulur; bu tür yalnızca dağıtım/altyapı kurumları içindir.
/// </summary>
public enum InstitutionType
{
    Electricity = 1,
    Water = 2,
    NaturalGas = 3
}
