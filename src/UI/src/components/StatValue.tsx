import { LocalizedNumber, LocalizedString, Unit } from "cs2/l10n"

import { kNoValue, kNoValueText } from "../constants"
import { VanillaLabel } from "../utils/locale"

interface StatValueProps {
    value: number
    unit: Unit
}

// A district or group figure in its own unit, or a placeholder where there was nothing to report.
export const StatValue = ({ value, unit }: StatValueProps) =>
    value === kNoValue ? <>{kNoValueText}</> : <LocalizedNumber value={value} unit={unit} />

// A citizen-stat band, read out under the name the game itself gives that band.
export const ThresholdValue = ({ label }: { label: VanillaLabel | null }) =>
    label === null ? <>{kNoValueText}</> : <LocalizedString id={label.id} fallback={label.fallback} />
