<script setup>
import { ref, onMounted } from 'vue'

const BASE = import.meta.env.VITE_API_URL ?? ''

async function apiFetch(path) {
  const res = await fetch(`${BASE}${path}`)
  if (!res.ok) throw new Error(`HTTP ${res.status}: ${path}`)
  return res.json()
}

// Mirrors Api.Org.AttendancePolicy.DeriveAttendees
function deriveAttendees(members, observers, observerParticipation) {
  const emails = new Set(members.map(m => m.email.toLowerCase()))
  if (observerParticipation === 'Invited') {
    for (const o of observers) emails.add(o.email.toLowerCase())
  }
  return [...emails].sort()
}

function formatRrule(rrule) {
  const p = Object.fromEntries(rrule.split(';').map(s => s.split('=')))
  const FREQ = { DAILY: 'Daily', WEEKLY: 'Weekly', MONTHLY: 'Monthly' }
  const DAYS = { MO: 'Mon', TU: 'Tue', WE: 'Wed', TH: 'Thu', FR: 'Fri', SA: 'Sat', SU: 'Sun' }
  const freq = FREQ[p.FREQ] ?? p.FREQ
  const interval = p.INTERVAL ? ` · every ${p.INTERVAL} weeks` : ''
  const days = p.BYDAY
    ? ` · ${p.BYDAY.split(',').map(d => DAYS[d] ?? d).join(', ')}`
    : ''
  return `${freq}${interval}${days}`
}

const loading = ref(true)
const error = ref(null)
const teams = ref([])

onMounted(async () => {
  try {
    const rawTeams = await apiFetch('/teams')
    teams.value = await Promise.all(
      rawTeams.map(async team => {
        const [members, observers, meetings] = await Promise.all([
          apiFetch(`/teams/${team.id}/members`),
          apiFetch(`/teams/${team.id}/observers`),
          apiFetch(`/teams/${team.id}/meetings`),
        ])
        return {
          ...team,
          members,
          observers,
          meetings: meetings.map(m => ({
            ...m,
            attendees: deriveAttendees(members, observers, m.observerParticipation),
          })),
        }
      })
    )
  } catch (e) {
    error.value = e.message
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="shell">
    <header class="site-header">
      <h1>Team Calendar Sync</h1>
      <p class="subtitle">Read-only view of synced team meetings and membership</p>
    </header>

    <main>
      <p v-if="loading" class="state-msg">Loading…</p>
      <p v-else-if="error" class="state-msg error">{{ error }}</p>

      <template v-else>
        <section>
          <h2>Meetings</h2>
          <div class="grid">
            <template v-for="team in teams" :key="team.id">
              <article v-for="meeting in team.meetings" :key="meeting.id" class="card">
                <div class="card-top">
                  <h3>{{ meeting.title }}</h3>
                  <span class="badge">{{ team.name }}</span>
                </div>
                <dl class="meta">
                  <dt>Schedule</dt>
                  <dd>{{ formatRrule(meeting.recurrence) }}</dd>
                  <dt>Observers</dt>
                  <dd>
                    <span :class="['pill', meeting.observerParticipation === 'Invited' ? 'pill-green' : 'pill-grey']">
                      {{ meeting.observerParticipation === 'Invited' ? 'Invited' : 'Read-only' }}
                    </span>
                  </dd>
                </dl>
                <div class="attendees">
                  <p class="list-label">Attendees ({{ meeting.attendees.length }})</p>
                  <ul>
                    <li v-for="email in meeting.attendees" :key="email">{{ email }}</li>
                  </ul>
                </div>
              </article>
            </template>
          </div>
        </section>

        <section>
          <h2>Teams</h2>
          <div class="grid">
            <article v-for="team in teams" :key="team.id" class="card">
              <div class="card-top">
                <h3>{{ team.name }}</h3>
              </div>
              <p class="team-desc">{{ team.description }}</p>
              <div class="people">
                <div>
                  <p class="list-label">Members ({{ team.members.length }})</p>
                  <ul class="person-list">
                    <li v-for="m in team.members" :key="m.email">
                      <span class="person-name">{{ m.name }}</span>
                      <span class="person-email">{{ m.email }}</span>
                      <span class="pill pill-blue">{{ m.role }}</span>
                    </li>
                  </ul>
                </div>
                <div>
                  <p class="list-label">Observers ({{ team.observers.length }})</p>
                  <ul class="person-list">
                    <li v-for="o in team.observers" :key="o.email">
                      <span class="person-name">{{ o.name }}</span>
                      <span class="person-email">{{ o.email }}</span>
                    </li>
                  </ul>
                </div>
              </div>
            </article>
          </div>
        </section>
      </template>
    </main>
  </div>
</template>

<style>
*, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0 }

body {
  font-family: system-ui, -apple-system, sans-serif;
  background: #f5f5f7;
  color: #1d1d1f;
  line-height: 1.5;
}

.shell {
  max-width: 1100px;
  margin: 0 auto;
  padding: 2rem 1.5rem;
}

.site-header { margin-bottom: 2.5rem }
.site-header h1 { font-size: 1.75rem; font-weight: 700 }
.subtitle { color: #6e6e73; margin-top: 0.25rem; font-size: 0.95rem }

section { margin-bottom: 3rem }
h2 { font-size: 1.2rem; font-weight: 600; margin-bottom: 1rem }

.grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1rem;
}

.card {
  background: #fff;
  border: 1px solid #e0e0e5;
  border-radius: 10px;
  padding: 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 0.875rem;
}

.card-top {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.5rem;
}
.card-top h3 { font-size: 1rem; font-weight: 600 }

.badge {
  flex-shrink: 0;
  font-size: 0.7rem;
  font-weight: 500;
  background: #e8f0fe;
  color: #1a56db;
  padding: 2px 8px;
  border-radius: 99px;
  white-space: nowrap;
}

.meta {
  display: grid;
  grid-template-columns: max-content 1fr;
  gap: 0.2rem 0.75rem;
  font-size: 0.85rem;
}
.meta dt { color: #6e6e73; font-weight: 500 }

.pill {
  display: inline-block;
  font-size: 0.72rem;
  padding: 1px 8px;
  border-radius: 99px;
  font-weight: 500;
}
.pill-green { background: #d1fae5; color: #065f46 }
.pill-grey  { background: #f3f4f6; color: #374151 }
.pill-blue  { background: #dbeafe; color: #1e40af }

.list-label {
  font-size: 0.72rem;
  font-weight: 600;
  color: #6e6e73;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  margin-bottom: 0.35rem;
}

.attendees ul { list-style: none; font-size: 0.875rem }
.attendees li { color: #374151; padding: 1px 0 }

.team-desc { font-size: 0.875rem; color: #6e6e73 }

.people { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem }

.person-list { list-style: none }
.person-list li {
  display: flex;
  flex-direction: column;
  padding: 4px 0;
  border-bottom: 1px solid #f5f5f7;
}
.person-name { font-size: 0.875rem; font-weight: 500 }
.person-email { font-size: 0.75rem; color: #6e6e73 }

.state-msg { padding: 3rem; text-align: center; color: #6e6e73 }
.state-msg.error { color: #dc2626 }
</style>
