import { LocalizedString } from "cs2/l10n"

import { VanillaLabel } from "../utils/locale"
import { kNoValue } from "../constants"

interface ThresholdValueProps {
    label: VanillaLabel | null
}

// A citizen-stat band, read out under the name the game itself gives that band.
export const ThresholdValue = ({ label }: ThresholdValueProps) =>
    label === null ? <>{kNoValue}</> : <LocalizedString id={label.id} fallback={label.fallback} />
