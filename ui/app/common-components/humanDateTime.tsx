import dayjs from "dayjs";
import AdvancedFormat from 'dayjs/plugin/advancedFormat'
import Utc from 'dayjs/plugin/Utc'
import Timezone from 'dayjs/plugin/Timezone'

dayjs.extend(Utc);
dayjs.extend(Timezone);
dayjs.extend(AdvancedFormat);

const format = "dddd, MMMM D, YYYY [@] h:mma z";
const browserTz = dayjs.tz.guess();

type HumanDateTimeProps = {
  isoDateTime?: string | Date;
};

const HumanDateTime = ({ isoDateTime }: HumanDateTimeProps) => {
  if (!isoDateTime) {
    return;
  }

  const local = dayjs.utc(isoDateTime).tz(browserTz);
  return (local.format(format));
}


export default HumanDateTime;