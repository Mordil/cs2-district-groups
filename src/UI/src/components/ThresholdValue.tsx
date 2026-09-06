import { LocalizedString } from "cs2/l10n"

import { VanillaLabel } from "../utils/locale"

// Stands in for a band with nothing behind it, when a group or district has no residents to average.
const kNoValue = "–"

interface ThresholdValueProps {
    label: VanillaLabel | null
}

// A citizen-stat band, read out under the name the game itself gives that band.
export const ThresholdValue = ({ label }: ThresholdValueProps) =>
    label === null ? <>{kNoValue}</> : <LocalizedString id={label.id} fallback={label.fallback} />
