import { LocalizedString } from "cs2/l10n"

import { VanillaLabel } from "../utils/locale"

/*
    A component rather than a hook so it stays legal anywhere a ReactNode is accepted - inside a
    conditional branch, or inside an element built in an event handler - and so it re-translates
    if the player's language changes while it is on screen.
*/
export const GameText = ({ label }: { label: VanillaLabel }) => (
    <LocalizedString id={label.id} fallback={label.fallback} />
)
