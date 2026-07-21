<script lang="ts">
  import { navigateLink } from '../router'

  export let href: string
  export let className = ''
</script>

<a {href} class={className} onclick={(event) => navigateLink(event, href)}><slot /></a>
