import { Unit } from "cs2/l10n"

import { VanillaLabel, VanillaLocale } from "../utils/locale"

// What a service building's own places are called, and the unit they read out in.
export interface Places {
    label: VanillaLabel
    unit: Unit
}

const kStudents: Places = { label: VanillaLocale.students, unit: Unit.Integer }

// Indexed by GroupServiceType - order must match the C# enum. Civic holds buildings of every type, so it has nothing to name.
const kPlaces: (Places | null)[] = [
    null,
    { label: VanillaLocale.prisoners, unit: Unit.Integer },
    // Shelters read out their capacity alone, with no occupancy share to name
    null,
    { label: VanillaLocale.patients, unit: Unit.Integer },
    { label: VanillaLocale.deceased, unit: Unit.Integer },
    { label: VanillaLocale.storedGarbage, unit: Unit.Weight },
    kStudents,
    kStudents,
    kStudents,
    kStudents,
    { label: VanillaLocale.mail, unit: Unit.Integer },
]

// What a building of this service type calls its own places, or null for a type with none to name.
export const placesOf = (type: number): Places | null => kPlaces[type] ?? null
