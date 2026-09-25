import { LocaleKey, useTranslation } from "../utils/locale"

// A mod-owned display string, translated where it renders so the column and card tables can stay plain data.
export const ModText = ({ label }: { label: LocaleKey }) => <>{useTranslation()(label)}</>
