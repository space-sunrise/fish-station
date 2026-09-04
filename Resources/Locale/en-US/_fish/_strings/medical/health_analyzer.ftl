health-analyzer-window-vitals-title = Vital signs
health-analyzer-window-damage-title = Damage

health-analyzer-window-treatment-title = Treatment recommendations
health-analyzer-window-treatment-none = No treatment is currently required.
health-analyzer-window-treatment-unsupported = No treatment recommendations are available for this patient.
health-analyzer-window-treatment-entry = { $condition }: { $reagent }
health-analyzer-window-treatment-entry-present = { $condition }: { $reagent } (already present: { $amount } u)
health-analyzer-window-treatment-damage = { $damage } ({ $amount } damage)
health-analyzer-window-treatment-critical = Critical condition stabilization
health-analyzer-window-treatment-bleeding = Active bleeding
health-analyzer-window-treatment-low-blood = Low blood level ({ $amount }%)
health-analyzer-window-treatment-dead-target = Before defibrillation, reduce total damage below { $threshold }. More than { $amount } damage must be healed.
health-analyzer-window-treatment-dead-unrevivable = Resuscitation is impossible. Medication cannot restore this patient.
health-analyzer-window-treatment-cryo-required = Cryogenic medicines require a body temperature of { $temperature } °C or lower.
health-analyzer-window-treatment-dead-brute-burn = Brute and burn damage
health-analyzer-window-treatment-dead-next = Use a defibrillator after reducing the damage below the revival threshold.
health-analyzer-window-treatment-dead-specialized = { $damage }: treatment before resuscitation requires specialized supplies. No suitable medicine made with ordinary chemistry is available in these recommendations.
health-analyzer-window-treatment-warning = Check active reagents and overdose limits before administering medicine.
health-analyzer-window-treatment-razorium-warning = OOC: Do not combine bicaridine, lacerinol, bruizine, or puncturase. Mixing any two produces dangerous razorium.
health-analyzer-window-treatment-active-amount = In blood: { $amount } u

health-analyzer-window-reagents-title = Active reagents
health-analyzer-window-reagents-none = No foreign reagents detected.
health-analyzer-window-reagents-entry = { $reagent }: { $amount } u
health-analyzer-window-reagents-amount = { $amount } u

health-analyzer-window-section-expanded = ▾ { $title }
health-analyzer-window-section-collapsed = ▸ { $title }
health-analyzer-window-trend-improving = ↓ { $amount }
health-analyzer-window-trend-worsening = ↑ { $amount }
health-analyzer-window-trend-unchanged = →
health-analyzer-window-trend-tooltip = Damage change since the previous active scan of this patient.

health-analyzer-window-medication-present = Already administered. Check damage trends before administering more.
health-analyzer-window-medication-threshold = Adverse-effect threshold: { $threshold } u.
health-analyzer-window-medication-threshold-tooltip = This is a recognized harmful-effect threshold based only on this substance's blood quantity. Other side effects and interactions may occur below it.
health-analyzer-window-medication-threshold-unknown = No universal threshold identified. Check the medicine's guidebook entry.
health-analyzer-window-medication-near = Near the { $threshold } u threshold. Do not automatically administer more.
health-analyzer-window-medication-overdose = The { $threshold } u threshold has been reached. Overdose risk: do not administer more.
health-analyzer-window-medication-alert-near = { $reagent }: { $amount } u. Near the adverse-effect threshold ({ $threshold } u).
health-analyzer-window-medication-alert-overdose = { $reagent }: { $amount } u. Adverse-effect threshold reached ({ $threshold } u). Do not administer more.
