import { LocalizedString } from "cs2/l10n"

import { LocaleKey, VanillaLabel, useTranslation } from "../utils/locale"

// A display string the game itself ships.
export const GameText = ({ label }: { label: VanillaLabel }) => (
    <LocalizedString id={label.id} fallback={label.fallback} />
)

// A mod-owned display string, translated where it renders so the column and card tables can stay plain data.
export const ModText = ({ label }: { label: LocaleKey }) => <>{useTranslation()(label)}</>
