import dayjs from "dayjs";
import advancedFormat from 'dayjs/plugin/advancedFormat'
import utc from 'dayjs/plugin/utc'
import timezone from 'dayjs/plugin/timezone'

dayjs.extend(utc);
dayjs.extend(timezone);
dayjs.extend(advancedFormat);

const defaultFormat = "dddd, MMMM D, YYYY [@] h:mma z";
const browserTz = dayjs.tz.guess();

type HumanDateTimeProps = {
  isoDateTime?: string | Date;
  format?: string
};

const LocalDateTime = ({ isoDateTime, format }: HumanDateTimeProps) => {
  if (!isoDateTime) {
    return;
  }

  const local = dayjs.utc(isoDateTime).tz(browserTz);
  return (local.format(format ?? defaultFormat));
}

export default LocalDateTime;